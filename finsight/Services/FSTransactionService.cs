using Finsight.Command;
using Finsight.Interface;
using Finsight.Models;

namespace Finsight.Service
{
    public class FSTransactionService
    {
        private readonly FSCurrencyConverter currencyConverter;
        private readonly FSICategoryRepository categoryRepository;
        private readonly FSITransactionRepository transactionRepository;
        public FSTransactionService(
            FSCurrencyConverter currencyConverter,
            FSICategoryRepository categoryRepository,
            FSITransactionRepository transactionRepository
        )
        {
            this.transactionRepository = transactionRepository;
            this.currencyConverter = currencyConverter;
            this.categoryRepository = categoryRepository;
        }
        public async Task<FSTransactionModel> CreateTransactionAsync(string userId, FSCreateTransactionCommand command)
        {
            var category = await categoryRepository.GetByIdAsync(userId, command.CategoryId);
            if (!string.IsNullOrEmpty(command.SubCategoryId) && category.SubCategories?.Find(sc => sc.CategoryId == command.SubCategoryId) == null)
            {
                throw new Exception($"Sub category id {command.SubCategoryId} does not belong to provided category ${category.Id}");
            }
            FSTransactionModel transaction = new()
            {
                Id = Guid.NewGuid().ToString(),
                Amount = await currencyConverter.ConvertToUSD(command),
                BaseAmount = command.Amount,
                CategoryId = command.CategoryId,
                Comment = command.Comment,
                Currency = command.Currency ?? FSSupportedCurrencies.USD,
                Date = command.Date ?? DateTime.Now,
                Mode = command.Mode,
                SubCategoryId = command.SubCategoryId,
                SubType = command.SubType,
                UpdatedAt = DateTime.Now,
                Type = category.Type
            };
            //await transactionRepository.AddAsync(userId, transaction);
            return transaction;
        }
    }
}