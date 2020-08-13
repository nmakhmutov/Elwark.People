using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elwark.People.Domain.SeedWork;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public class Links : ValueObject, IDictionary<LinksType, Uri?>
    {
        private readonly SortedDictionary<LinksType, Uri?> _dictionary;

        public Links() =>
            _dictionary = new SortedDictionary<LinksType, Uri?>();

        public Links(IDictionary<LinksType, Uri?> links) =>
            _dictionary = new SortedDictionary<LinksType, Uri?>(links);

        public IEnumerator<KeyValuePair<LinksType, Uri?>> GetEnumerator() =>
            _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public void Add(KeyValuePair<LinksType, Uri?> item) =>
            _dictionary.Add(item.Key, item.Value);

        public void Clear() =>
            _dictionary.Clear();

        public bool Contains(KeyValuePair<LinksType, Uri?> item) =>
            _dictionary.Contains(item);

        public void CopyTo(KeyValuePair<LinksType, Uri?>[] array, int arrayIndex) =>
            _dictionary.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<LinksType, Uri?> item) =>
            _dictionary.Remove(item.Key);

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(LinksType key, Uri? value) =>
            _dictionary.Add(key, value);

        public bool ContainsKey(LinksType key) =>
            _dictionary.ContainsKey(key);

        public bool Remove(LinksType key) =>
            _dictionary.Remove(key);

        public bool TryGetValue(LinksType key, out Uri? value) =>
            _dictionary.TryGetValue(key, out value);

        public Uri? this[LinksType key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public ICollection<LinksType> Keys => _dictionary.Keys;
        public ICollection<Uri?> Values => _dictionary.Values;

        protected override IEnumerable<object?> GetAtomicValues() =>
            _dictionary.Cast<object?>();
    }
}