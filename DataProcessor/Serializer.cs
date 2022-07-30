namespace BookShop.DataProcessor
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using Data;
    using Data.Models;
    using Data.Models.Enums;
    using ExportDto;
    using Newtonsoft.Json;
    using Formatting = Newtonsoft.Json.Formatting;

    public class Serializer
    {
        public static string ExportMostCraziestAuthors(BookShopContext context)
        {
            //Export Most Craziest Authors
           // Select all authors along with their books.
           // Select their name in format first name + ' ' + last name.
           // For each book select its name and price formatted to the second digit after the decimal point.
           // Order the books by price in descending order. Finally sort all authors by
           // book count descending and then by author full name.
           //NOTE: Before the orders, materialize the query
           //(This is issue by Microsoft in InMemory database library)!!!
            var books = context
                .Authors
                .Select(a => new 
                {
                    AuthorName = a.FirstName + " " + a.LastName,
                    Books = a.AuthorsBooks
                        .OrderByDescending(p => p.Book.Price)
                        .Select(b => new
                        {
                            BookName = b.Book.Name,
                            BookPrice = b.Book.Price.ToString("F2")
                        })
                        .ToArray()
                })
                .ToArray()
                .OrderByDescending(b => b.Books.Length)
                .ThenBy(b => b.AuthorName)
                .ToArray();

            return JsonConvert.SerializeObject(books, Formatting.Indented);
        }

        public static string ExportOldestBooks(BookShopContext context, DateTime date)
        {
            //Export Oldest Books
            //Export top 10 oldest books that are published before the given date and are of type science.
            //For each book select its name, date (in format "d") and pages.
            //Sort them by pages in descending order and then by date in descending order.
            //NOTE: Before the orders, materialize the query
            //(This is issue by Microsoft in InMemory database library)!!!
            var books = context.Books
                .Where(d => d.PublishedOn < date && d.Genre == Genre.Science)
                .OrderByDescending(x => x.Pages)
                .ThenByDescending(x => x.PublishedOn)
                .Select(b => new ExportBookDto
                {
                    BookName = b.Name,
                    Date = b.PublishedOn.ToString("d", CultureInfo.InvariantCulture),
                    Pages = b.Pages,
                })
                .Take(10)
                .ToArray();

            var result = SerializeXml(books, "Books");

            return result;
        }

        private static string SerializeXml<T>(T[] objects, string root)
        {
            var serializer = new XmlSerializer(typeof(T[]), new XmlRootAttribute(root));
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName() });

            var sb = new StringBuilder();

            serializer.Serialize(new StringWriter(sb), objects, namespaces);

            return sb.ToString().TrimEnd();
        }
    }
}