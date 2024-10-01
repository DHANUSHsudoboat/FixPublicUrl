

using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;

namespace GeneratePublicLink.services
{
    public class PublicUrl
    {
        public async Task<IEntity> Generate(string? relativeUrl,string Resource,long ItemId,IWebMClient MClient)
        {
            try
            {
                //Generating new Public Link
                IEntity newPublicLink = await MClient.EntityFactory.CreateAsync("M.PublicLink");

                //using the (deleted) public url details
                newPublicLink.SetPropertyValue("RelativeUrl", relativeUrl);
                newPublicLink.SetPropertyValue("Resource", Resource);

                //creating the 'AssetToPublicLink' relation with newly created public url  and add item's -->assetID in this relation.
                var relation = newPublicLink.GetRelation<IChildToManyParentsRelation>("AssetToPublicLink");
                relation.Parents.Add(ItemId);
            
                
                return newPublicLink;
            }
            catch (Exception ex) {
                Console.WriteLine($"{ex.Message} in generating new Url for {ItemId}");
                return null;
            }
        }
    }
}
