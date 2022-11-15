using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc.EF.Helper;
using CMI.Contract.Common.Entities;

namespace CMI.Access.Sql.Viaduc.EF
{
    public class ManuelleKorrekturAccess : IManuelleKorrekturAccess
    {
        private readonly ViaducDb dbContext;
        private readonly AccessHelper accessHelper;

        public ManuelleKorrekturAccess(ViaducDb dbContext, AccessHelper accessHelper)
        {
            this.accessHelper = accessHelper;
            DbInterception.Add(new ViaducDbCommandInterceptor());
            this.dbContext = dbContext;
        }

        public ViaducDb Context => dbContext;

        public Task<List<VManuelleKorrekturDto>> GetAllManuelleKorrekturen()
        {
            return Task.FromResult(dbContext.VManuelleKorrekturen.ToDtosWithRelated(1).ToList());
        }

        public Task<ManuelleKorrekturDto> GetManuelleKorrektur(int manuelleKorrekturId)
        {
            var manuelleKorrektur = dbContext.ManuelleKorrekturen.FirstOrDefault(m => m.ManuelleKorrekturId.Equals(manuelleKorrekturId))
                .ToDtoWithRelated(1);
            return Task.FromResult(manuelleKorrektur);
        }

        public Task<ManuelleKorrekturDto> GetManuelleKorrektur(Func<ManuelleKorrektur, bool> searchPredicate)
        {
            var record = dbContext.ManuelleKorrekturen.FirstOrDefault(searchPredicate).ToDtoWithRelated(1);
            return Task.FromResult(record);
        }

        /// <summary>
        /// If the veId or signature does not exist, a new ManuelleKorrektur can be added with this id 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<bool> CheckCanInsertManuelleKorrektur(string id)
        {
            return int.TryParse(id, out int veId) ? 
                Task.FromResult(!dbContext.ManuelleKorrekturen.Any(m => m.VeId == veId)) : 
                Task.FromResult(!dbContext.ManuelleKorrekturen.Any(m => m.Signatur == id));
        }

        public async Task<ManuelleKorrekturDto> Publizieren(int manuelleKorrekturId, string userId)
        {
            var manuelleKorrektur = dbContext.ManuelleKorrekturen.FirstOrDefault(m => m.ManuelleKorrekturId.Equals(manuelleKorrekturId));
            var username = accessHelper.GetUserNameFromId(userId);
            // ReSharper disable once PossibleNullReferenceException
            manuelleKorrektur.ManuelleKorrekturStatusHistories.Add(new ManuelleKorrekturStatusHistory
            {
                ErzeugtVon = username,
                ErzeugtAm = DateTime.Now,

                ManuelleKorrekturId = manuelleKorrektur.ManuelleKorrekturId,
                Anonymisierungsstatus = (int) AnonymisierungsStatusEnum.Published
            });
            manuelleKorrektur.Anonymisierungsstatus = (int) AnonymisierungsStatusEnum.Published;
            manuelleKorrektur.GeändertAm = DateTime.Now;
            manuelleKorrektur.GeändertVon = username;
            await dbContext.SaveChangesAsync();
            return await GetManuelleKorrektur(manuelleKorrekturId);
        }

        public Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto value, string userId)
        {
            ManuelleKorrektur item;
            var username = accessHelper.GetUserNameFromId(userId);

            if (value.ManuelleKorrekturId <= 0)
            {
                var check = CheckCanInsertManuelleKorrektur(value.VeId.ToString()).Result;
                if (!check)
                {
                    throw new ArgumentException("Schon vorhanden");
                }
                item = new ManuelleKorrektur
                {
                     ErzeugtVon = username,
                     ErzeugtAm = DateTime.Now
                   
                };
                dbContext.ManuelleKorrekturen.AddObject(item);
            }
            else
            {   
                item = dbContext.ManuelleKorrekturen.FirstOrDefault(m => m.ManuelleKorrekturId.Equals(value.ManuelleKorrekturId));
                if (item != null)
                {
                    item.GeändertVon = username;
                    item.GeändertAm = DateTime.Now;
                }
            }

            if (item != null)
            {
                UpdateStatusHistory(item, value, username);

                item.Aktenzeichen = value.Aktenzeichen;
                item.AnonymisiertZumErfassungszeitpunk = value.AnonymisiertZumErfassungszeitpunk;
                item.Entstehungszeitraum = value.Entstehungszeitraum;
                item.Hierachiestufe = value.Hierachiestufe;
                item.Kommentar = value.Kommentar;
                item.Publikationsrechte = value.Publikationsrechte;
                item.Schutzfristende = value.Schutzfristende.Kind == DateTimeKind.Utc ? value.Schutzfristende.ToLocalTime() : value.Schutzfristende;
                item.Schutzfristverzeichnung = value.Schutzfristverzeichnung;
                item.Signatur = value.Signatur;
                item.Titel = value.Titel;
                item.VeId = value.VeId;
                item.ZuständigeStelle = value.ZuständigeStelle;
                item.ZugänglichkeitGemässBGA = value.ZugänglichkeitGemässBGA;
                item.Anonymisierungsstatus = value.Anonymisierungsstatus;

                SaveManuelleKorrekturFelder(item, value.ManuelleKorrekturFelder);
                
                dbContext.SaveChanges();
            }

            return Task.FromResult(item.ToDtoWithRelated(1));
        }

        private void UpdateStatusHistory(ManuelleKorrektur item, ManuelleKorrekturDto newValue, string username)
        {
            if (item.ManuelleKorrekturId <= 0 || item.Anonymisierungsstatus != newValue.Anonymisierungsstatus)
            {
                item.ManuelleKorrekturStatusHistories.Add(new ManuelleKorrekturStatusHistory
                {
                    Anonymisierungsstatus = newValue.Anonymisierungsstatus,
                    ErzeugtAm = DateTime.Now,
                    ErzeugtVon = username
                });
            }
        }

        public Task DeleteManuelleKorrektur(int manuelleKorrekturId)
        {
            var mauelleKorrektur = dbContext.ManuelleKorrekturen.FirstOrDefault(d => d.ManuelleKorrekturId == manuelleKorrekturId);
            if (mauelleKorrektur != null)
            {
                dbContext.ManuelleKorrekturen.DeleteObject(mauelleKorrektur);
                dbContext.SaveChanges();
            }

            return Task.CompletedTask;
        }

        public async Task BatchDeleteManuelleKorrekturen(int[] manuelleKorrekturIds)
        {
            foreach (var manuelleKorrekturId in manuelleKorrekturIds) 
            {
                await DeleteManuelleKorrektur(manuelleKorrekturId);
            }
        }

        private void SaveManuelleKorrekturFelder(ManuelleKorrektur item, List<ManuelleKorrekturFeldDto> fields)
        {
            foreach (var field in fields)
            {
                var itemField = item.ManuelleKorrekturFelder.FirstOrDefault(i => i.Feldname.Equals(field.Feldname));
                if (itemField != null)
                {
                    // Update
                    itemField.Automatisch = field.Automatisch;
                    itemField.Manuell = field.Manuell;
                    itemField.Original = field.Original;
                }
                else
                {
                    // Insert
                    item.ManuelleKorrekturFelder.Add(field.ToEntity());
                }

            }
        }

    }
}
