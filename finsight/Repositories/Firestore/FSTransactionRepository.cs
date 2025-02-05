using Finsight.Models;
using Finsight.Utilities;
using Google.Cloud.Firestore;
using Finsight.Interface;
using Finsight.Query;

namespace Finsight.Repositories
{
    public class FSTransactionRepository : FSITransactionRepository
    {
        private readonly FirestoreDb firestore;
        public FSTransactionRepository(FirestoreDb firestore)
        {
            this.firestore = firestore;
        }
        public async IAsyncEnumerable<FSTransactionModel> FetchAsync(string userId, FSTransactionQuery query)
        {
            var snapshot = await firestore.Collection(CONSTANTS.USER_COLLECTION).Document(userId).Collection(CONSTANTS.TRANSACTION_COLLECTION)
            .WhereGreaterThanOrEqualTo("date", query.From)
            .WhereLessThanOrEqualTo("date", query.To)
            .GetSnapshotAsync();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                FSTransactionModel transaction = FirestoreMapper.MapTo<FSTransactionModel>(document);
                yield return transaction;
            }
        }
    }
}