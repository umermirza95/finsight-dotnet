using Finsight.Models;
using Finsight.Utilities;
using Google.Cloud.Firestore;
using Finsight.Interface;
using Finsight.Query;
using Finsight.Command;
using Finsight.Service;

namespace Finsight.Repositories
{
    public class FSTransactionRepository : FSITransactionRepository
    {
        private readonly FirestoreDb firestore;
        private readonly FSCurrencyConverter CurrencyConverter;
        public FSTransactionRepository(FirestoreDb firestore, FSCurrencyConverter currencyConverter)
        {
            this.firestore = firestore;
            CurrencyConverter = currencyConverter;
        }
        public async IAsyncEnumerable<FSTransactionModel> FetchAsync(string userId, FSTransactionQuery query)
        {
            var snapshot = await firestore.Collection(CONSTANTS.USER_COLLECTION).Document(userId).Collection(CONSTANTS.TRANSACTION_COLLECTION)
            .WhereGreaterThanOrEqualTo("date", query.From)
            .WhereLessThanOrEqualTo("date", query.To)
            .OrderByDescending("date")
            .GetSnapshotAsync();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                FSTransactionModel transaction = FirestoreMapper.MapTo<FSTransactionModel>(document);
                yield return transaction;
            }
        }

        public async Task AddAsync(string userId, FSTransactionModel transaction)
        {
            await firestore.Collection(CONSTANTS.USER_COLLECTION)
            .Document(userId)
            .Collection(CONSTANTS.TRANSACTION_COLLECTION)
            .Document(transaction.Id)
            .SetAsync(transaction);
        }
    }
}