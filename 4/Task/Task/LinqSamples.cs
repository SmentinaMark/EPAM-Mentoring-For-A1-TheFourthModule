// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;

// Version Mad01

namespace SampleQueries
{
	[Title("LINQ Module")]
	[Prefix("Linq")]
	public class LinqSamples : SampleHarness
	{

		private DataSource dataSource = new DataSource();

		[Category("Restriction Operators")]
		[Title("MyTask 001")]
		[Description("This method shows a list of all customers whose total turnover is greater than the X value ")]
		public void Linq001()
		{
			int x = 25000;
			var clients = dataSource.Customers.Where(w => w.Orders.Sum(q => q.Total) > x);
			foreach (var p in clients)
			{
				ObjectDumper.Write(p.CustomerID);
			}
		}

        [Category("Restriction Operators")]
        [Title("MyTask 002")]
        [Description("This method shows all Customers with the names of Suppliers which are located in the same country and city ")]
        public void Linq002()
        {
            var clients = dataSource.Customers.Select(x => new
            {
                x.CustomerID,
                sup = dataSource.Suppliers
                .Where(c => c.Country == x.Country && c.City == x.City),})
                .Where(c => c.sup.Count() > 0);

            ObjectDumper.Write($"Without group by");
            foreach (var p in clients)
            {
                ObjectDumper.Write($"{p.CustomerID}" + " - " + $"{string.Join("", p.sup.Select(s => s.SupplierName))}");
            }
            ObjectDumper.Write("------------------------------------------");
            ObjectDumper.Write($"With group by");

            var customers = dataSource.Customers
                .Join(dataSource.Suppliers,
                c => new { c.Country, c.City },
                s => new { s.Country, s.City },
                (c, s) => new
                {
                    CustomerInfo = $"{c.CustomerID} Country {c.Country}, City {c.City}",
                    SupplierInfo = $"{s.SupplierName} Country {s.Country} City {s.City}",
                })
                .GroupBy(x => x.CustomerInfo)
                .ToDictionary(g => g.Key, g => g.Select(x => x.SupplierInfo));

            foreach (var c in customers)
            {
                foreach (var v in c.Value)
                {
                    ObjectDumper.Write($"Customer{c.Key } |"+ v);
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("MyTask 003")]
        [Description("This method shows a list of all customers who had an order price greater then X ")]
        public void Linq003()
        {
            int x = 10000;
            var clients = dataSource.Customers.Where(w => w.Orders.Any(r=>r.Total > x));
            foreach (var p in clients)
            {
                ObjectDumper.Write($"{ p.CustomerID} {string.Join("", p.Orders.Select(r=>r.Total))}");
            }
        }

        [Category("Restriction Operators")]
        [Title("MyTask 004")]
        [Description("This method shows a list of all customers with the date of the first order ")]
        public void Linq004()
        {
            var clients = dataSource.Customers
               .Where(x => x.Orders.Count() > 0)
               .Select(x => new
               {
                   x.CustomerID,
                   date = x.Orders.OrderBy(q => q.OrderDate).Select(o => o.OrderDate).Min()
               });
            foreach (var p in clients)
            {
                ObjectDumper.Write($"{ p.CustomerID} {p.date.ToShortDateString()}");
            }
        }

        [Category("Restriction Operators")]
        [Title("MyTask 005")]
        [Description("This method shows a list of all customers with the min. date, month, total sum. ")]
        public void Linq005()
        {
            var clients = dataSource.Customers
               .Where(x => x.Orders.Count() > 0)
               .Select(x => new
               {
                   x.CustomerID,
                   sum =  x.Orders.Sum(q=>q.Total),
                   date = x.Orders.OrderBy(q => q.OrderDate).Select(o => o.OrderDate).Min()

               }).OrderByDescending(r => r.date.Year) 
                 .ThenByDescending(r=>r.date.Month)
                 .ThenByDescending(r=>r.sum)
                 .ThenByDescending(r=>r.CustomerID);
            foreach (var p in clients)
            {
                ObjectDumper.Write($"{ p.date.Year } { p.date.Month } { p.sum } { p.CustomerID} ");
            }
        }

        [Category("Restriction Operators")]
        [Title("MyTask 006")]
        [Description("This method shows a list of all customers who has non-numeric postal code or region not filled or" +
            "phone number without an operator")]
        public void Linq006()
        {
            var clients = dataSource.Customers
               .Where(x => x.PostalCode !=null && !x.PostalCode.All(c => c < '0' || c > '9') || x.Region == null || !x.Phone.StartsWith("(") );

            foreach (var c in clients)
            {
                ObjectDumper.Write($"{c.CustomerID}" + " | " + $"{c.Region}" + " | " + $"{ c.PostalCode}" + " | " + $"{ c.Phone}");
            }
        }
        [Category("Restriction Operators")]
        [Title("MyTask 007")]
        [Description("This method shows a list of all customers with the min. date, month, total sum. ")]
        public void Linq007()
        {
            var products = dataSource.Products.GroupBy(x => x.Category)
            .Select(x => new 
            {
                Category = x.Key,
                InStockprod = x.GroupBy(t=>t.UnitsInStock > 0)
                .Select(q=> new
                {
                        PrInStock = q.Key,
                        PriceProd = q.OrderBy(t=>t.UnitPrice)
                })
            });
            foreach (var q in products)
            {
                ObjectDumper.Write("--------------------------------------------------------------");
                ObjectDumper.Write($"Category: {q.Category}\n");
                foreach (var w in q.InStockprod)
                {
                    ObjectDumper.Write($"\tIn stock: {w.PrInStock}");
                    foreach (var e in w.PriceProd)
                    {
                        ObjectDumper.Write($"\t\t {e.ProductName} Price: {string.Format("{0:0.00}", e.UnitPrice)}");
                    }
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("MyTask 008")]
        [Description("This method shows a list of all product int 3 groups (Cheap, Average Price, Expensive)")]
        public void Linq008()
        {
            int LowPrice = 50;
            int AvaragePrice = 100;

            var products = dataSource.Products.GroupBy(x => x.UnitPrice <= LowPrice ? "Cheap"
            : x.UnitPrice <= AvaragePrice ? "Average Price" : "Expensive");

            foreach (var q in products)
            {
                ObjectDumper.Write($"{q.Key}");
                foreach (var item in q)
                {
                    ObjectDumper.Write($"\t{item.ProductName}" + " - " + $"{string.Format("{0:0.00}", item.UnitPrice)}");
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("MyTask 009")]
        [Description("This method shows a list of average profit for each city, average number of orders")]
        public void Linq009()
        {
            var cust = dataSource.Customers.GroupBy(q => q.City)
                .Select(q => new
                {
                    City = q.Key,
                    AverSum = q.Where(w=>w.Orders.Count()>0).Select(r => r.Orders.Select(t => t.Total).Average()).Average(),
                    AverOrd = q.Where(w=>w.Orders.Count()>0).Select(r => r.Orders.Count()).Average()
                }) ;

            foreach (var q in cust)
            {
                ObjectDumper.Write($"{q.City}" + "|" + $"{string.Format("{0:0.00}",q.AverOrd)}" + "|" + 
                                   $" {string.Format("{0:0.00}", q.AverSum)}");
            }
        }

        [Category("Restriction Operators")]
        [Title("MyTask 010")]
        [Description("This method shows a list of average value of the activity by Month, Years, Month and Years")]
        public void Linq010()
        {
            var cust = dataSource.Customers.Select(q => new
            {
                q.CustomerID,

                StatByMonth = q.Orders.GroupBy(w => w.OrderDate.Month)
                    .Select(e=>new { Month = e.Key,Count = e.Count()}),

                StatByYears = q.Orders.GroupBy(w => w.OrderDate.Year)
                    .Select(e => new { Year= e.Key, Count = e.Count()}),

                MonthYears = q.Orders.GroupBy(w => w.OrderDate.Year)
                    .Select(e => new { Year = e.Key, Count = e.Count() })
            });

            foreach (var q in cust)
            {
                ObjectDumper.Write($"{q.CustomerID}");
                foreach (var w in q.StatByMonth)
                {
                    ObjectDumper.Write($"Month - {w.Month} Count = {w.Count}");
                }
                foreach (var e in q.StatByYears)
                {
                    ObjectDumper.Write($"Year - {e.Year} Count = {e.Count}");
                }
            }
        }


    }
    
    
}
