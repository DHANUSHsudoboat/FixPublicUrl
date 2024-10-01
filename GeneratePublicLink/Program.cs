using GeneratePublicLink.services;
using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.WebClient.Authentication;
using Stylelabs.M.Sdk.WebClient.Extensions;
using System.IO;
using System.Net;
using System.Text;

public class Program()
{
    
    public async static Task Main(string[] args)
    {

        PublicUrl _publicUrl = new PublicUrl();


        //get the image assets
        var query = Query.CreateQuery(entities => (from e in entities
                                                   where e.DefinitionName == "M.Asset"
                                                   && e.Parent("ContentRepositoryToAsset") == 734
                                                   && e.Parent("AssetTypeToAsset").In(6303943, 6303948, 6303946, 6305026, 6303942, 6305024, 12822834, 6303949, 6305025)
                                                   && e.Property("DigitalAssetClass") == "IKEAO.DigitalAssetCall.Image"
                                                   && e.Parent("FinalLifeCycleStatusToAsset") == 544
                                                   && e.Property("ReleaseDate") < DateTime.Now
                                                   select e));


   
        //QA link
        Uri endpoint = new Uri("https://ikea-q-002.sitecorecontenthub.cloud");

        OAuthPasswordGrant oauth = new OAuthPasswordGrant
        {
            ClientId = "IKEAAzF",
            ClientSecret = "546260bf-a4ff-47be-9d9b-2d7bc925d2e8",
            UserName = "Integration-worker-1",
            Password = "Worker-1"
        };

        IWebMClient MClient = MClientFactory.CreateMClient(endpoint, oauth);

        var result = await MClient.Querying.QueryAsync(query);

        File.Delete("C:\\Users\\selav\\OneDrive\\Desktop\\IDAM\\ChangePublicLink\\GeneratePublicLink\\GeneratePublicLink\\logs\\logs.txt");
        using (FileStream fs = File.Create("C:\\Users\\selav\\OneDrive\\Desktop\\IDAM\\ChangePublicLink\\GeneratePublicLink\\GeneratePublicLink\\logs\\logs.txt"))
        {

            foreach (IEntity item in result.Items)
            {
                try
                {
                    //get the AssetTOPublicLink in the Items --> relation
                    IParentToManyChildrenRelation AssetTopublicLinks = await item.GetRelationAsync<IParentToManyChildrenRelation>("AssetToPublicLink");

                    //get the public link ids --> thumbnail id,preview id,... 
                    var assetIDs = AssetTopublicLinks.GetIds();

                    //get the details of those public link ids
                    var publicLinksIDs = await MClient.Entities.GetManyAsync(assetIDs);

                    using (HttpClient client = new HttpClient())
                    {
                        foreach (IEntity publicLink in publicLinksIDs)
                        {
                            //try to access the item's  public url 
                            var response = await client.GetAsync(publicLink.GetPublicLink().ToString());

                            //if it return 404 ,then need to regenerate that url 
                            if (response.StatusCode == HttpStatusCode.NotFound)
                            {

                                //get the URl Path of the public url and Resource(thumbnail or preview or original)
                                var relativeurl = publicLink.GetPropertyValue<string>("RelativeUrl");
                                var resource = publicLink.GetPropertyValue<string>("Resource");

                                IEntity newPublicLink = await _publicUrl.Generate(relativeurl, resource, (long)item.Id, MClient);

                                if (newPublicLink == null)
                                {
                                    continue;
                                }
                                //delete that public link
                                await MClient.Entities.DeleteAsync((long)publicLink.Id);
                                //save the newly generating public url 
                                await MClient.Entities.SaveAsync(newPublicLink);
                            }
                        }
                    }
                }
                catch (Exception ex) {

                        byte[] info = new UTF8Encoding(true).GetBytes($"assetID = {item.Id} failed  due to {ex.Message}\n");
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                
                }

            }
        }
    }
}