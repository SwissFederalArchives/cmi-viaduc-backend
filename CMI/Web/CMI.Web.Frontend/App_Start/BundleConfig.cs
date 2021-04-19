using System.IO;
using System.Web.Optimization;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            var dirHelper = DirectoryHelper.Instance;

            // static bundles
            var staticRoot = StringHelper.AddToString("~", "/", dirHelper.StaticDefaultPath);
            if (Directory.Exists(WebHelper.MapPathIfNeeded(staticRoot)))
            {
                bundles.Add(new ScriptBundle("~/static/js").Include(staticRoot + "js/*.js"));
                bundles.Add(
                    new ScriptBundle("~/static/css").Include(
                        staticRoot + "css/*.css",
                        staticRoot + "css/*.png"
                    )
                );
                // bundles.Add(
                //     new ScriptBundle("~/bundles/fonts").Include(
                //         staticRoot + "fonts/*.ttf", 
                //         staticRoot + "fonts/*.eot", 
                //         staticRoot + "fonts/*.svg", 
                //         staticRoot + "fonts/*.woff",
                //         staticRoot + "fonts/*.woff2"
                //     )
                //);
            }


            // client bundles
            var clientRoot = StringHelper.AddToString("~", "/", dirHelper.ClientDefaultPath);
            if (Directory.Exists(WebHelper.MapPathIfNeeded(clientRoot)))
            {
                WebHelper.SetupClientDefaultBundleConfig(bundles);
            }
        }
    }
}