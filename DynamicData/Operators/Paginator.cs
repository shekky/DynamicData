﻿#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Kernel;

#endregion

namespace DynamicData.Operators
{
    internal sealed class Paginator<TObject, TKey> 
    {
        #region Fields

        private IKeyValueCollection<TObject, TKey> _all =new KeyValueCollection<TObject, TKey>();
        private IKeyValueCollection<TObject, TKey> _current =new KeyValueCollection<TObject, TKey>();
        private IPageRequest _request=null;
        private readonly FilteredIndexCalulator<TObject, TKey> _changedCalulator = new FilteredIndexCalulator<TObject, TKey>();
        private bool _isLoaded;

        #endregion

        #region Construction

        public Paginator()
        {
            _request =  PageRequest.Default;
            _isLoaded = false;
        }

        #endregion

        #region Pagination
        
        public IPagedChangeSet<TObject, TKey> Paginate(IPageRequest parameters)
        {
            if (parameters==null || parameters.Page < 0 || parameters.Size < 1)
            {
                return null;
            }

            if (parameters.Size == _request.Size && parameters.Page == _request.Page)
                return null;

            _request = parameters;


            return Paginate();
        }

        public IPagedChangeSet<TObject, TKey> Update(ISortedChangeSet<TObject, TKey> updates)
        {
            _isLoaded = true;
            _all = updates.SortedItems;
            return Paginate();
        }

        private IPagedChangeSet<TObject, TKey> Paginate(ISortedChangeSet<TObject, TKey> updates=null)
        {
            if (_isLoaded == false) return null;
            if (_request == null) return null;
     
            var previous = _current;

            int pages = CalculatePages();
            int page = _request.Page > pages ? pages : _request.Page;
            int skip = _request.Size * (page - 1);

            var paged = _all.Skip(skip)
                                 .Take(_request.Size)
                                 .ToList();

            _current = new KeyValueCollection<TObject, TKey>(paged, _all.Comparer, SortReason.DataChanged,_all.Optimisations);

            //check for changes within the current virtualised page.  Notify if there have been changes or if the overall count has changed
            var notifications = _changedCalulator.Calculate(_current, previous, updates);
            if (notifications.Count == 0 && (previous.Count != _current.Count))
            {
                return null;
            }
            var response = new PageResponse(_request.Size, _all.Count, page, pages);

          return new PagedChangeSet<TObject, TKey>(_current,notifications,response);
    
        }

        private int CalculatePages()
        {
            if (_request.Size >= _all.Count)
            {
                return 1;
            }

            int pages = _all.Count / _request.Size;
            int overlap = _all.Count % _request.Size;

            if (overlap == 0)
            {
                return pages;
            }
            return pages + 1;
        }

        #endregion
    }
}