// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
#if NET6_0_OR_GREATER
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
#else
using Microsoft.Restier.AspNet.Model;

#endif

#if EF6
using Microsoft.Restier.EntityFramework;
#else
using Microsoft.Restier.EntityFrameworkCore;
#endif

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// A testable API that implements an Entity Framework model and has secondary operations
    /// against a SQL 2017 LocalDB database.
    /// </summary>
    public class LibraryApi : EntityFrameworkApi<LibraryContext>
    {

        #region Constructors

        public LibraryApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        #endregion

        #region API Methods

        [UnboundOperation(OperationType = OperationType.Action, EntitySet = "Books")]
        public Book CheckoutBook(Book book)
        {
            if (book is null)
            {
                throw new ArgumentNullException(nameof(book));
            }
            Console.WriteLine($"Id = {book.Id}");
            book.Title += " | Submitted";
            return book;
        }

        [BoundOperation(IsComposable = true)]
        public IQueryable<Book> DiscontinueBooks(IQueryable<Book> books)
        {
            if (books is null)
            {
                throw new ArgumentNullException(nameof(books));
            }
            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Discontinued";
            });
            return books;
        }

        [UnboundOperation]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
        public IQueryable<Book> FavoriteBooks()
        {
            var publisher = new Publisher
            {
                Id = "123",
                Addr = new Address
                {
                    Street = "Publisher Way",
                    Zip = "12345"
                }
            };

            foreach (var book in new Book[]
            {
                new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "The Cat in the Hat Comes Back",
                    Publisher = publisher,
                    IsActive = true
                },
                new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "If You Give a Mouse a Cookie",
                    Publisher = publisher
                }
            })
            {
                publisher.Books.Add(book);
            }

            return publisher.Books.AsQueryable();
        }

        [UnboundOperation]
        public Book PublishBook(bool IsActive)
        {
            Console.WriteLine($"IsActive = {IsActive}");
            return new Book
            {
                Id = Guid.NewGuid(),
                Title = "The Cat in the Hat"
            };
        }

        [UnboundOperation]
        public Book PublishBooks(int Count)
        {
            Console.WriteLine($"Count = {Count}");
            return new Book
            {
                Id = Guid.NewGuid(),
                Title = "The Cat in the Hat Comes Back"
            };
        }

        [BoundOperation(OperationType = OperationType.Action)]
        public Publisher PublishNewBook(Publisher publisher, Guid bookId)
        {
            var book = DbContext.Set<Book>().Find(bookId);

            publisher.Books.Add(book);
            DbContext.SaveChanges();

            return publisher;
        }

        [BoundOperation(IsComposable = true, EntitySetPath = "publisher/Books")]
        public IQueryable<Book> PublishedBooks(Publisher publisher)
        {
            var test = publisher.Id;
            return FavoriteBooks();
        }

        [UnboundOperation]
        public Book SubmitTransaction(Guid Id)
        {
            Console.WriteLine($"Id = {Id}");
            return new Book
            {
                Id = Id,
                Title = "Atlas Shrugged"
            };
        }

        #endregion

        #region Restier Interceptors

        /// <summary>
        /// Limits the results of <see cref="Book" /> queries by a pre-determined set of criteria.
        /// </summary>
        internal protected IQueryable<Book> OnFilterBooks(IQueryable<Book> entitySet) => entitySet.Where(c => c.IsActive);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal protected bool CanUpdateEmployee() => false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="books"></param>
        internal protected void OnExecutingDiscontinueBooks(IQueryable<Book> books)
        {
            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Intercepted";
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="books"></param>
        internal protected void OnExecutedDiscontinueBooks(IQueryable<Book> books)
        {
            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Intercepted";
            });
        }

        /// <summary>
        /// Ensures that incoming Books get assigned an ID.
        /// </summary>
        /// <param name="book"></param>
        internal protected void OnInsertingBook(Book book)
        {
            if (book.Id == Guid.Empty)
            {
                book.Id = Guid.NewGuid();
            }
        }

        /// <summary>
        /// Ensures that publishers that are being updated get the correct Audit flag set.
        /// </summary>
        /// <param name="publisher"></param>
        internal protected void OnUpdatingPublisher(Publisher publisher)
        {
            publisher.LastUpdated = DateTimeOffset.Now;
        }

        #endregion

    }

}