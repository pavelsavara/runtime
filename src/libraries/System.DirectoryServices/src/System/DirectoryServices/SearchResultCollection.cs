// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using INTPTR_INTPTRCAST = System.IntPtr;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Contains the instances of <see cref='System.DirectoryServices.SearchResult'/> returned during a
    /// query to the Active Directory hierarchy through <see cref='System.DirectoryServices.DirectorySearcher'/>.
    /// </devdoc>
    public class SearchResultCollection : MarshalByRefObject, ICollection, IEnumerable, IDisposable
    {
        private IntPtr _handle;
        private UnsafeNativeMethods.IDirectorySearch? _searchObject;
        private ArrayList? _innerList;
        private bool _disposed;
        private readonly DirectoryEntry _rootEntry;       // clone of parent entry object
        private const string ADS_DIRSYNC_COOKIE = "fc8cb04d-311d-406c-8cb9-1ae8b843b418";
        private readonly IntPtr _adsDirsynCookieName = Marshal.StringToCoTaskMemUni(ADS_DIRSYNC_COOKIE);
        private const string ADS_VLV_RESPONSE = "fc8cb04d-311d-406c-8cb9-1ae8b843b419";
        private readonly IntPtr _adsVLVResponseName = Marshal.StringToCoTaskMemUni(ADS_VLV_RESPONSE);
        internal DirectorySearcher srch;

        internal SearchResultCollection(DirectoryEntry root, IntPtr searchHandle, string[] propertiesLoaded, DirectorySearcher srch)
        {
            _handle = searchHandle;
            PropertiesLoaded = propertiesLoaded;
            Filter = srch.Filter;
            _rootEntry = root;
            this.srch = srch;
        }

        public SearchResult this[int index] => (SearchResult)InnerList[index]!;

        public int Count => InnerList.Count;

        internal string? Filter { get; }

        private ArrayList InnerList
        {
            get
            {
                if (_innerList == null)
                {
                    var eagerList = new ArrayList();
                    var enumerator = new ResultsEnumerator(
                        this,
                        _rootEntry.GetUsername(),
                        _rootEntry.GetPassword(),
                        _rootEntry.AuthenticationType);

                    while (enumerator.MoveNext())
                        eagerList.Add(enumerator.Current);

                    _innerList = eagerList;
                }

                return _innerList;
            }
        }

        internal UnsafeNativeMethods.IDirectorySearch SearchObject =>
            _searchObject ??= (UnsafeNativeMethods.IDirectorySearch)_rootEntry.AdsObject;   // get it only once

        /// <devdoc>
        /// Gets the handle returned by IDirectorySearch::ExecuteSearch, which was called
        /// by the DirectorySearcher that created this object
        /// </devdoc>
        public IntPtr Handle
        {
            get
            {
                //The handle is no longer valid since the object has been disposed.
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _handle;
            }
        }

        /// <devdoc>
        /// Gets a read-only collection of the properties  specified on <see cref='System.DirectoryServices.DirectorySearcher'/> before the
        /// search was executed.
        /// </devdoc>
        public string[] PropertiesLoaded { get; }

        internal byte[] DirsyncCookie => RetrieveDirectorySynchronizationCookie();

        internal DirectoryVirtualListView VLVResponse => RetrieveVLVResponse();

        private unsafe byte[] RetrieveDirectorySynchronizationCookie()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the dirsync cookie back
            AdsSearchColumn column = default;
            AdsSearchColumn* pColumn = &column;
            SearchObject.GetColumn(Handle, _adsDirsynCookieName, (INTPTR_INTPTRCAST)pColumn);
            try
            {
                AdsValue* pValue = column.pADsValues;
                byte[] value = (byte[])new AdsValueHelper(*pValue).GetValue();

                return value;
            }
            finally
            {
                try
                {
                    SearchObject.FreeColumn((INTPTR_INTPTRCAST)pColumn);
                }
                catch (COMException)
                {
                }
            }
        }

        private unsafe DirectoryVirtualListView RetrieveVLVResponse()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the vlv response back
            AdsSearchColumn column = default;
            AdsSearchColumn* pColumn = &column;
            SearchObject.GetColumn(Handle, _adsVLVResponseName, (INTPTR_INTPTRCAST)pColumn);
            try
            {
                AdsValue* pValue = column.pADsValues;
                DirectoryVirtualListView value = (DirectoryVirtualListView)new AdsValueHelper(*pValue).GetVlvValue();
                return value;
            }
            finally
            {
                try
                {
                    SearchObject.FreeColumn((INTPTR_INTPTRCAST)pColumn);
                }
                catch (COMException)
                {
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_handle != (IntPtr)0 && _searchObject != null && disposing)
                {
                    // NOTE: We can't call methods on SearchObject in the finalizer because it
                    // runs on a different thread. The IDirectorySearch object is STA, so COM must create
                    // a proxy stub to marshal the call back to the original thread. Unfortunately, the
                    // IDirectorySearch interface cannot be registered, because it is not automation
                    // compatible. Therefore the QI for IDirectorySearch on this thread fails, and we get
                    // an InvalidCastException. The conclusion is that the user simply must call Dispose
                    // on this object.

                    _searchObject.CloseSearchHandle(_handle);

                    _handle = (IntPtr)0;
                }

                if (disposing)
                    _rootEntry.Dispose();

                if (_adsDirsynCookieName != (IntPtr)0)
                    Marshal.FreeCoTaskMem(_adsDirsynCookieName);

                if (_adsVLVResponseName != (IntPtr)0)
                    Marshal.FreeCoTaskMem(_adsVLVResponseName);

                _disposed = true;
            }
        }

        ~SearchResultCollection() => Dispose(false);

        public IEnumerator GetEnumerator()
        {
            if (_innerList != null)
                return new AlreadyReadResultsEnumerator(_innerList);
            else
                return new ResultsEnumerator(this, _rootEntry.GetUsername(), _rootEntry.GetPassword(), _rootEntry.AuthenticationType);
        }

        public bool Contains(SearchResult result) => InnerList.Contains(result);

        public void CopyTo(SearchResult[] results, int index)
        {
            InnerList.CopyTo(results, index);
        }

        public int IndexOf(SearchResult result) => InnerList.IndexOf(result);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        /// <summary>Provides an enumerator implementation for the array of <see cref="SearchResult"/>.</summary>
        private struct AlreadyReadResultsEnumerator : IEnumerator
        {
            private readonly IEnumerator _innerEnumerator;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="innerList">The ArrayList to enumerate.</param>
            internal AlreadyReadResultsEnumerator(ArrayList innerList)
            {
                _innerEnumerator = innerList.GetEnumerator();
            }

            /// <summary>
            /// Current <see cref="SearchResult"/> value of the <see cref="IEnumerator"/>
            /// </summary>
            public SearchResult Current
            {
                get
                {
                    return (SearchResult)(_innerEnumerator.Current);
                }
            }

            /// <summary>
            /// Advances the enumerator to the next <see cref="SearchResult"/> element of the arrray.
            /// </summary>
            public bool MoveNext()
            {
                return _innerEnumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to the beginning of the array.
            /// </summary>
            public void Reset()
            {
                _innerEnumerator.Reset();
            }

            /// <summary>
            /// Current <see cref="object"/> of the <see cref="IEnumerator"/>
            /// </summary>
            object IEnumerator.Current => Current;
        }

        /// <devdoc>
        /// Supports a simple ForEach-style iteration over a collection.
        /// </devdoc>
        private sealed class ResultsEnumerator : IEnumerator
        {
            private readonly NetworkCredential? _parentCredentials;
            private readonly AuthenticationTypes _parentAuthenticationType;
            private readonly SearchResultCollection _results;
            private bool _initialized;
            private SearchResult? _currentResult;
            private bool _eof;

            internal ResultsEnumerator(SearchResultCollection results, string? parentUserName, string? parentPassword, AuthenticationTypes parentAuthenticationType)
            {
                if (parentUserName != null && parentPassword != null)
                    _parentCredentials = new NetworkCredential(parentUserName, parentPassword);

                _parentAuthenticationType = parentAuthenticationType;
                _results = results;
                _initialized = false;
            }

            /// <devdoc>
            /// Gets the current element in the collection.
            /// </devdoc>
            public SearchResult Current
            {
                get
                {
                    if (!_initialized || _eof)
                        throw new InvalidOperationException(SR.DSNoCurrentEntry);

                    return _currentResult ??= GetCurrentResult();
                }
            }

            private unsafe SearchResult GetCurrentResult()
            {
                SearchResult entry = new SearchResult(_parentCredentials, _parentAuthenticationType);
                int hr = 0;
                IntPtr pszColumnName = (IntPtr)0;
                hr = _results.SearchObject.GetNextColumnName(_results.Handle, (INTPTR_INTPTRCAST)(&pszColumnName));
                while (hr == 0)
                {
                    try
                    {
                        AdsSearchColumn column = default;
                        AdsSearchColumn* pColumn = &column;
                        _results.SearchObject.GetColumn(_results.Handle, pszColumnName, (INTPTR_INTPTRCAST)pColumn);
                        try
                        {
                            int numValues = column.dwNumValues;
                            AdsValue* pValue = column.pADsValues;
                            object[] values = new object[numValues];
                            for (int i = 0; i < numValues; i++)
                            {
                                values[i] = new AdsValueHelper(*pValue).GetValue();
                                pValue++;
                            }
                            entry.Properties.Add(Marshal.PtrToStringUni(pszColumnName)!, new ResultPropertyValueCollection(values));
                        }
                        finally
                        {
                            try
                            {
                                _results.SearchObject.FreeColumn((INTPTR_INTPTRCAST)pColumn);
                            }
                            catch (COMException)
                            {
                            }
                        }
                    }
                    finally
                    {
                        Interop.Activeds.FreeADsMem(pszColumnName);
                    }
                    hr = _results.SearchObject.GetNextColumnName(_results.Handle, (INTPTR_INTPTRCAST)(&pszColumnName));
                }

                return entry;
            }

            /// <devdoc>
            /// Advances the enumerator to the next element of the collection
            /// and returns a Boolean value indicating whether a valid element is available.
            /// </devdoc>
            public bool MoveNext()
            {
                DirectorySynchronization? tempsync = null;
                DirectoryVirtualListView? tempvlv = null;
                int errorCode = 0;

                if (_eof)
                    return false;

                _currentResult = null;

                if (!_initialized)
                {
                    int hr = _results.SearchObject.GetFirstRow(_results.Handle);

                    if (hr != UnsafeNativeMethods.S_ADS_NOMORE_ROWS)
                    {
                        //throw a clearer exception if the filter was invalid
                        if (hr == UnsafeNativeMethods.INVALID_FILTER)
                            throw new ArgumentException(SR.Format(SR.DSInvalidSearchFilter, _results.Filter));
                        if (hr != 0)
                            throw COMExceptionHelper.CreateFormattedComException(hr);

                        _eof = false;
                        _initialized = true;
                        return true;
                    }

                    _initialized = true;
                }

                while (true)
                {
                    // clear the last error first
                    CleanLastError();
                    errorCode = 0;

                    int hr = _results.SearchObject.GetNextRow(_results.Handle);
                    //  SIZE_LIMIT_EXCEEDED occurs when we supply too generic filter or small SizeLimit value.
                    if (hr == UnsafeNativeMethods.S_ADS_NOMORE_ROWS || hr == UnsafeNativeMethods.SIZE_LIMIT_EXCEEDED)
                    {
                        // need to make sure this is not the case that server actually still has record not returned yet
                        if (hr == UnsafeNativeMethods.S_ADS_NOMORE_ROWS)
                        {
                            hr = GetLastError(ref errorCode);
                            // get last error call failed, we need to bail out
                            if (hr != 0)
                                throw COMExceptionHelper.CreateFormattedComException(hr);
                        }

                        // not the case that server still has result, we are done here
                        if (errorCode != Interop.Errors.ERROR_MORE_DATA)
                        {
                            // get the dirsync cookie as we finished all the rows
                            if (_results.srch.directorySynchronizationSpecified)
                                tempsync = _results.srch.DirectorySynchronization;

                            // get the vlv response as we finished all the rows
                            if (_results.srch.directoryVirtualListViewSpecified)
                                tempvlv = _results.srch.VirtualListView;

                            _results.srch.searchResult = null;

                            _eof = true;
                            _initialized = false;
                            return false;
                        }
                        else
                        {
                            uint temp = (uint)errorCode;
                            temp = (temp & 0x0000FFFF) | (7 << 16) | 0x80000000;
                            throw COMExceptionHelper.CreateFormattedComException((int)temp);
                        }
                    }
                    //throw a clearer exception if the filter was invalid
                    if (hr == UnsafeNativeMethods.INVALID_FILTER)
                        throw new ArgumentException(SR.Format(SR.DSInvalidSearchFilter, _results.Filter));
                    if (hr != 0)
                        throw COMExceptionHelper.CreateFormattedComException(hr);

                    _eof = false;
                    return true;
                }
            }

            /// <devdoc>
            /// Resets the enumerator back to its initial position before the first element in the collection.
            /// </devdoc>
            public void Reset()
            {
                _eof = false;
                _initialized = false;
            }

            object IEnumerator.Current => Current;

            private void CleanLastError()
            {
                Interop.Activeds.ADsSetLastError(Interop.Errors.ERROR_SUCCESS, null, null);
            }

            private int GetLastError(ref int errorCode)
            {
                char[] errorBuffer = Array.Empty<char>();
                char[] nameBuffer = Array.Empty<char>();
                errorCode = Interop.Errors.ERROR_SUCCESS;
                return Interop.Activeds.ADsGetLastError(out errorCode, errorBuffer, errorBuffer.Length, nameBuffer, nameBuffer.Length);
            }
        }
    }
}
