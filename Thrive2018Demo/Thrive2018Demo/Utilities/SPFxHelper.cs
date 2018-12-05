using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Reflection;
using System.IO;
using System.IO.Packaging;

namespace Thrive2018Demo.Utilities
{
    public static class SPFxHelper
    {
        public static Guid[] SPFxWebParts =
        {
            new Guid("8e582ca2-40e5-41d9-b9b7-d45138915e70"),
            new Guid("67f2208a-b80a-4a55-9bec-34a1e3ee50f1")
        };

        public static void DeploySPFxWebPartInAppCatalog(string cSiteUrl)
        {
            using (SPSite site = new SPSite(cSiteUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    SPList list = web.FindListByName("AppCatalog");

                    foreach (Guid spfxItem in SPFxWebParts)
                    {
                        SPQuery query = new SPQuery
                        {
                            Query = "<Where><Eq><FieldRef Name='AppProductID'/><Value Type='Text'>" + spfxItem.ToString("B") + "</Value></Eq></Where>",
                        };

                        SPListItem li = list.GetItems(query)[0];
                        li["IsDefaultAppMetadataLocale"] = true;
                        li["IsAppPackageEnabled"] = true;
                        li["IsClientSideSolutionDeployed"] = true;
                        li.Update();
                    }
                }
            }
        }

        public static void LoadAndInstallSPFxAppPackages(string dstWebUrl)
        {
            //Assembly ass = Assembly.Load("Microsoft.SharePoint");
            Assembly ass = Assembly.LoadFile(@"C:\Program Files\Common Files\microsoft shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.dll");
            Type spAppType = ass.GetType("Microsoft.SharePoint.Administration.SPApp");
            MethodInfo method = spAppType.GetMethod("CreateAppUsingPackageMetadata", BindingFlags.NonPublic | BindingFlags.Static);

            Type spSecurityType = ass.GetType("Microsoft.SharePoint.SPSecurity");
            Type spAppAllowRunAsSystemAccountScopeType = spSecurityType.GetNestedType("SPAppAllowRunAsSystemAccountScope", BindingFlags.NonPublic | BindingFlags.Static);
            ConstructorInfo spAppAllowRunASSystemAcountConstructor = spAppAllowRunAsSystemAccountScopeType.GetConstructors()[0];
            MethodInfo spAppAllowRunASSystemAcountDispose = spAppAllowRunAsSystemAccountScopeType.GetMethod("Dispose");

            using (SPSite site = new SPSite("http://sp2016/sites/appcatalog"))
            {
                using (SPWeb web = site.RootWeb)
                {
                    SPList list = web.FindListByName("AppCatalog");

                    foreach (Guid spfxItem in SPFxWebParts)
                    {
                        SPQuery query = new SPQuery
                        {
                            Query = "<Where><Eq><FieldRef Name='AppProductID'/><Value Type='Text'>" + spfxItem.ToString("B") + "</Value></Eq></Where>",
                        };

                        SPListItem li = list.GetItems(query)[0];

                        byte[] fileByteArray = li.File.OpenBinary();

                        using (MemoryStream ms = new MemoryStream())
                        {
                            ms.Write(fileByteArray, 0, fileByteArray.Length);

                            using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                            {
                                Uri manifestUri = new Uri("/AppManifest.xml", UriKind.Relative);
                                Uri partUri = PackUriHelper.CreatePartUri(manifestUri);
                                PackagePart part = package.GetPart(partUri);

                                string content = null;

                                using (Stream partStream = part.GetStream(FileMode.Open, FileAccess.Read))
                                {
                                    using (StreamReader reader = new StreamReader(partStream))
                                    {
                                        content = reader.ReadToEnd();
                                        if (content.IndexOf("<StartPage>") == -1)
                                            content = content.Replace("</Title>", "</Title><StartPage>/</StartPage>");
                                        if (content.IndexOf("<AppPrincipal>") == -1)
                                            content = content.Replace("</Properties>", "</Properties><AppPrincipal><Internal></Internal></AppPrincipal>");
                                    }
                                }

                                using (Stream partStream = part.GetStream(FileMode.Open, FileAccess.Write))
                                {
                                    using (StreamWriter writer = new StreamWriter(partStream))
                                    {
                                        writer.Write(content);
                                        writer.Flush();
                                    }
                                }
                            }

                            ms.Seek(0, SeekOrigin.Begin);

                            object spAppAllowRunASSystemAcountObject = null;
                            try
                            {
                                spAppAllowRunASSystemAcountObject = spAppAllowRunASSystemAcountConstructor.Invoke(null);

                                using (SPSite dstSite = new SPSite(dstWebUrl))
                                {
                                    using (SPWeb dstWeb = dstSite.OpenWeb())
                                    {
                                        SPApp spApp = null;
                                        try
                                        {
                                            spApp = (SPApp)method.Invoke(null, new object[] { ms, dstWeb, 2, false, null, null });
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.HResult == -2146232828)
                                            {
                                                foreach (SPWeb dstWebTemp in dstSite.AllWebs)
                                                {
                                                    var spAppInstances = dstWebTemp.GetAppInstancesByProductId(spfxItem);
                                                    if (spAppInstances.Count > 0)
                                                    {
                                                        spApp = spAppInstances.First().App;
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if (spApp != null)
                                        {
                                            var appInstanceID = spApp.CreateAppInstance(dstWeb);
                                            var appInstance = dstWeb.GetAppInstanceById(appInstanceID);
                                            appInstance.Install();
                                        }
                                    }
                                }
                            }
                            catch (Exception _ex)
                            {
                                Logger.ToLog(_ex, "Error SPFxHelper");
                                throw new SPException(_ex.Message);
                            }
                            finally
                            {
                                spAppAllowRunASSystemAcountDispose.Invoke(spAppAllowRunASSystemAcountObject, null);
                            }
                        }
                    }
                }
            }
        }
    }
}
