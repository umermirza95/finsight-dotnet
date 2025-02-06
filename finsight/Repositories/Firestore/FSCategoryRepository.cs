using Finsight.Models;
using Finsight.Utilities;
using Google.Cloud.Firestore;
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
        public async IAsyncEnumerable<FSCategoryModel> FetchAsync(string userId)
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

        public async Task<FSCategoryModel> GetByIdAsync(string userId, string categoryId)
        {

            var documentSnapshot = await firestore.Collection(CONSTANTS.USER_COLLECTION)
            .Document(userId)
            .Collection(CONSTANTS.CATEGORIES_COLLECTION)
            .Document(categoryId)
            .GetSnapshotAsync();
            if (!documentSnapshot.Exists)
            {
                throw new Exception($"Invalid category Id {categoryId}");
            }
            var category = FirestoreMapper.MapTo<FSCategoryModel>(documentSnapshot);
            category.SubCategories = [];
            var snapshot = await firestore.Collection(CONSTANTS.USER_COLLECTION).Document(userId).Collection(CONSTANTS.SUBCATEGPRIES_COLLECTION).GetSnapshotAsync();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                var subCategory = FirestoreMapper.MapTo<FSSubCategoryModel>(document);
                if (subCategory.CategoryId == categoryId)
                {
                    category.SubCategories.Add(subCategory);
                }
            }
            return category;
        }
    }
}