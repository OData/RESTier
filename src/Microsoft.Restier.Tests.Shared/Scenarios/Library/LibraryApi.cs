// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
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

    }

}