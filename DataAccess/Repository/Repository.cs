using DataAccess.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : Model
    {
        protected readonly IMongoCollection<T> _models;
        protected readonly DateTime _now;

        public Repository(BlockTanksStatsDatabaseSettings settings, string collectionName, DateTime now)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _models = database.GetCollection<T>(collectionName);

            // The AddTicks is necessary because now stores the time in greater accuracy than what is stored in the database (fractional milliseconds).
            // By subtracting the fractional part, we make sure that _now will contain the exact same time as what is stored in the database
            // This is necessary when we want to compare the timestamps in the database with _now afterwards.
            _now = now.AddTicks(-now.TimeOfDay.Ticks % 10000);
        }

        public virtual async Task<IEnumerable<T>> GetAsync(CancellationToken cancellation = default) =>
            await (await _models.FindAsync(_ => true, cancellationToken: cancellation)).ToListAsync(cancellation);

        public virtual async Task<T> CreateAsync(T model, CancellationToken cancellation = default)
        {
            model.Timestamp = _now;
            await _models.InsertOneAsync(model, cancellationToken: cancellation);
            return model;
        }

        public virtual async Task UpdateAsync(string id, T modelIn, CancellationToken cancellation = default)
        {
            modelIn.Timestamp = modelIn.Timestamp == default ? _now : modelIn.Timestamp;
            await _models.ReplaceOneAsync(model => model.Id == id, modelIn, cancellationToken: cancellation);
        }

        public virtual async Task RemoveAsync(T modelIn, CancellationToken cancellation = default) =>
            await _models.DeleteOneAsync(model => model.Id == modelIn.Id, cancellationToken: cancellation);

        public virtual async Task RemoveByIdAsync(string id, CancellationToken cancellation = default) =>
            await _models.DeleteOneAsync(mode => mode.Id == id, cancellationToken: cancellation);
    }
}
