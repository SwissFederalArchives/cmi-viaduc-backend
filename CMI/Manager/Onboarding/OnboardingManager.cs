using CMI.Access.Onboarding;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Onboarding.Properties;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Onboarding.Response;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Engine.MailTemplate;
using CMI.Utilities.Template;
using MassTransit;
using System.Globalization;

namespace CMI.Manager.Onboarding
{
    public interface IOnboardingManager
    {
        Task<StartProcessResult> StartOnboardingProcess(OnboardingModel onboardingModel);

        Task<bool> HandleOnboardingCallback(CallbackType callbackType, PostbackParameters parameters);
    }

    public class OnboardingManager : IOnboardingManager
    {
        private readonly IBus bus;
        private readonly IOnboardingConnector onboardingConnector;
        private readonly IUserDataAccess dataAccess;
        private readonly IParameterHelper parameterHelper;
        private readonly IMailHelper mailHelper;
        private readonly IDataBuilder dataBuilder;

        public OnboardingManager(IBus bus, IOnboardingConnector onboardingConnector, IUserDataAccess dataAccess, 
            IParameterHelper parameterHelper, IMailHelper mailHelper, IDataBuilder dataBuilder)
        {
            this.bus = bus;
            this.onboardingConnector = onboardingConnector;
            this.dataAccess = dataAccess;
            this.parameterHelper = parameterHelper;
            this.mailHelper = mailHelper;
            this.dataBuilder = dataBuilder;
        }

        public async Task<StartProcessResult> StartOnboardingProcess(OnboardingModel onboardingModel)
        {
            try
            {
                var payload = GetStartProcessData(onboardingModel);
                Log.Information("Sending onboarding request to fidentiy for user {userId} and payload {payload}", onboardingModel.UserId, payload);

                var url = await onboardingConnector.StartProcess(payload);
                return new StartProcessResult
                {
                    Success = true,
                    ProcessUrl = url
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while starting onboarding process");
                return new StartProcessResult
                {
                    ProcessUrl = null,
                    Success = false
                };
            }
        }

        public async Task<bool> HandleOnboardingCallback(CallbackType callbackType, PostbackParameters parameters)
        {
            switch (callbackType)
            {
                case CallbackType.Success:
                    return await Success(parameters);
                case CallbackType.Warn:
                    return await Warn(parameters);
                case CallbackType.Error:
                    return await Error(parameters);
                case CallbackType.Review:
                    return await Review(parameters);
                default:
                    throw new ArgumentOutOfRangeException(nameof(callbackType), callbackType, null);
            }
        }

        public async Task<bool> Warn(PostbackParameters parameters)
        {
            try
            {
                Log.Information("Warn has been called with userId {userId}", parameters.userId);

                var user = dataAccess.GetUserWitExtId(parameters.userId);
                if (user != null)
                {
                    await SendMailWithTemplate<IdentifizierungAdminWarnung>(user.Id);
                    await SendMailWithTemplate<IdentifizierungWarnung>(user.Id);
                }
                else
                {
                    Log.Information("User not found in Viaduc during warn callback.");
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on processing Warn callback.");
            }

            return false;
        }

        public async Task<bool> Error(PostbackParameters parameters)
        {
            try
            {
                Log.Information("Error callback has been called with parameter extId={extId}", parameters.extId);

                var user = dataAccess.GetUserWitExtId(parameters.userId);
                // Es dürften nur Ö2 Benutzer die Authentifizierung ausführen können und im Error callback ankommen.
                if (user?.RolePublicClient == AccessRoles.RoleOe2)
                {
                    var status = await onboardingConnector.GetProcessById(parameters.extId);
                    var rejectionReason = GetRejectReason(status);

                    dataAccess.UpdateRejectionReason(rejectionReason, user.Id);
                    await SendMailWithTemplate<IdentifizierungFehlgeschlagen>(user.Id);
                    return true;
                }

                Log.Information("Benutzer mit id {userId} existiert nicht in Viaduc oder Benutzer hat nicht die Role Ö2 - ignoriert.", parameters.userId);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on processing error callback call.");
            }
            return false;
        }

        public async Task<bool> Success(PostbackParameters parameters)
        {
            try
            {
                Log.Information("Success Callback has been called with parameter extId = {extId}", parameters.extId);

                var user = dataAccess.GetUserWitExtId(parameters.userId);
                if (user?.RolePublicClient == AccessRoles.RoleOe2)
                {
                    var status = await onboardingConnector.GetProcessById(parameters.extId);

                    // Überprüfung ist hier eigentlich nicht notwendig , der Callback wird nur aufgerufen bei success
                    if (status.Verification.State == "processed" && status.Verification.Decision.Process == "ok")
                    {
                        await ProcessUser(user, status);
                    }

                    return true;
                }

                Log.Information("Benutzer mit id {userId} existiert nicht in Viaduc oder Benutzer hat nicht die Role Ö2 - ignoriert.", parameters.userId);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on processing success callback call.");
            }
            return false;
        }

        public async Task<bool> Review(PostbackParameters parameters)
        {
            try
            {
                Log.Information("Review Callback has been called with parameter extId = {extId}", parameters.extId);

                var user = dataAccess.GetUserWitExtId(parameters.userId);
                if (user?.RolePublicClient == AccessRoles.RoleOe2)
                {
                    var status = await onboardingConnector.GetProcessById(parameters.extId);

                    // Review ist manuell auf ok gesetzt
                    if (status.Verification.State == "processed" && status.Verification.Decision.Process == "ok")
                    {
                        await ProcessUser(user, status);
                    }

                    // Review ist manuell auf ok gesetzt
                    if (status.Verification.State == "processed" && status.Verification.Decision.Process == "nok")
                    {
                        var rejectionReason = GetRejectReason(status);
                        dataAccess.UpdateRejectionReason(rejectionReason, user.Id);

                        await SendMailWithTemplate<IdentifizierungFehlgeschlagen>(user.Id);
                    }

                    return true;
                }

                Log.Information("Benutzer mit id {userId} existiert nicht in Viaduc oder Benutzer hat nicht die Role Ö2 - ignoriert.", parameters.userId);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on processing review callback call.");
            }
            return false;
        }
        
        private string GetRejectReason(Status status)
        {
            return $"{status.Verification.Quality} {status.Verification.Score} {status.Verification.CreatedAt}";
        }

        private async Task ProcessUser(User user, Status status)
        {
            Log.Information("Execute ProcessUser {id}",user.Id);
            user.FirstName = status.Customer.Firstname.IdValue;
            user.FamilyName = status.Customer.Name.IdValue;
            user.Birthday = DateTime.ParseExact(status.Customer.DateOfBirth.IdValue, "yyyy-MM-dd",CultureInfo.InvariantCulture);
            
            dataAccess.UpdateRejectionReason(null, user.Id);
            dataAccess.UpdateUser(user, null);
            Log.Information("User updated with {firstname} {familyname} {birthdate}", user.FirstName, user.FamilyName, user.Birthday);

            var data = await GetStatusReportPdf(status);
            dataAccess.SetIdentifierDocument(user.Id, data, AccessRoles.RoleOe3);

            await SendMailWithTemplate<IdentifizierungErfolgreich>(user.Id);
        }

        private async Task<byte[]> GetStatusReportPdf(Status status)
        {
            byte[] data = new byte[0];

            var report = status.MediaUris.FirstOrDefault(m => m.MediaType == "report-pdf");
            if (report != null)
            {
                data = await onboardingConnector.GetDocumentByUri(report.Uri);
            }
            return data;
        }

        private async Task SendMailWithTemplate<T>(string userId) where T : EmailTemplate
        {
            try
            {
                var template = parameterHelper.GetSetting<T>();
                var dataContext = dataBuilder
                    .AddUser(userId)
                    .Create();

                await mailHelper.SendEmail(bus, template, dataContext, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }

        private string GetStartProcessData(OnboardingModel model)
        {
            return JObject.FromObject(new
            {
                extId = model.ProcessId,
                lang = model.Language,
                customer = new
                {
                    userId = model.UserId,
                    name = model.Name,
                    firstname = model.Firstname,
                    nationality = model.Nationality,
                    dateOfBirth = model.DateOfBirth,
                    idType = model.IdType,
                    email = model.Email,
                },
                processSteps = new[]
                {
                    new { type = "DocumentRegistration"  },
                    new { type = "Selfie" }
                },
                noValidation = false,
                systemUrls = new[]
                {
                    new { url = $"{OnboardingSetting.Default.OnboardingCallbackSuccessUrl}", type = "callback", state = "success" },
                    new { url = $"{OnboardingSetting.Default.OnboardingCallbackWarnUrl}", type = "callback", state = "warn" },
                    new { url = $"{OnboardingSetting.Default.OnboardingCallbackErrorUrl}", type = "callback", state = "error" },
                    new { url = $"{OnboardingSetting.Default.OnboardingCallbackReviewUrl}", type = "review-callback", state = "success" }
                }
            }).ToString();
        }
    }
}
