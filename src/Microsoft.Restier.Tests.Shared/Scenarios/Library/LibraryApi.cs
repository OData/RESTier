﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.EntityFramework;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// A testable API that implements an Entity Framework model and has secondary operations
    /// against a SQL 2017 LocalDB database.
    /// </summary>
    public class LibraryApi : EntityFrameworkApi<LibraryContext>
    {

        public LibraryApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        [Operation]
        public Book PublishBook(bool IsActive)
        {
            Console.WriteLine($"IsActive = {IsActive}");
            return new Book
            {
                Id = Guid.NewGuid(),
                Title = "The Cat in the Hat"
            };
        }

        [Operation]
        public Book PublishBooks(int Count)
        {
            Console.WriteLine($"Count = {Count}");
            return new Book
            {
                Id = Guid.NewGuid(),
                Title = "The Cat in the Hat Comes Back"
            };
        }

        [Operation]
        [EnableQuery(AllowedQueryOptions=AllowedQueryOptions.All)]
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
                    Publisher = publisher
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

        [Operation(IsBound = true, IsComposable = true, EntitySet = "publisher/Books")]
        public IQueryable<Book> PublishedBooks(Publisher publisher)
        {
            var test = publisher.Id;
            return FavoriteBooks();
        }

        [Operation]
        public Book SubmitTransaction(Guid Id)
        {
            Console.WriteLine($"Id = {Id}");
            return new Book
            {
                Id = Id,
                Title = "Atlas Shrugged"
            };
        }

        [Operation(OperationType = OperationType.Action, EntitySet = "Books")]
        public Book CheckoutBook(Book book)
        {
            if (book == null)
            {
                throw new ArgumentNullException(nameof(book));
            }
            Console.WriteLine($"Id = {book.Id}");
            book.Title += " | Submitted";
            return book;
        }

        protected internal void OnExecutingDiscontinueBooks(IQueryable<Book> books)
        {
            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Intercepted";
            });
        }

        protected internal void OnExecutedDiscontinueBooks(IQueryable<Book> books)
        {
            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Intercepted";
            });
        }

        [Operation(IsBound = true, IsComposable = true)]
        public IQueryable<Book> DiscontinueBooks(IQueryable<Book> books)
        {
            if (books == null)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal bool CanUpdateEmployee() => false;

    }

}