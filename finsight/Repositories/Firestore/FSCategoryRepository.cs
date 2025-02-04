using Finsight.Models;
using Finsight.Utilities;
using Google.Cloud.Firestore;
using System.Threading.Tasks;
using Finsight.Interface;

namespace Finsight.Repositories
{
    public class FSCategoryRepository : FSICategoryRepository
    {
        private readonly FirestoreDb firestore;
        public FSCategoryRepository(FirestoreDb firestore)
        {
            this.firestore = firestore;
        }
        public async IAsyncEnumerable<FSCategoryModel> GetAllAsync(string userId)
        {
            var snapshot = await firestore.Collection(CONSTANTS.USER_COLLECTION).Document(userId).Collection(CONSTANTS.SUBCATEGPRIES_COLLECTION).GetSnapshotAsync();
            List<FSSubCategoryModel> subCategories = [];
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                subCategories.Add(FirestoreMapper.MapTo<FSSubCategoryModel>(document));
            }
            snapshot = await firestore.Collection(CONSTANTS.USER_COLLECTION).Document(userId).Collection(CONSTANTS.CATEGORIES_COLLECTION).GetSnapshotAsync();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                FSCategoryModel category = FirestoreMapper.MapTo<FSCategoryModel>(document);
                category.SubCategories = subCategories.FindAll((sc) => sc.CategoryId == category.Id);
                yield return category;
            }
        }
    }
}