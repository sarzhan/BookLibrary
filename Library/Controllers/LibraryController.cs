using Library.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Library.Controllers
{
    public class LibraryController : Controller
    {
        private string strConnection = ConfigurationManager.ConnectionStrings["LibraryConnectionString"].ConnectionString.ToString();
        private int pageSize = 5;
        private int pageNumMax = 0;
        // GET: Library

        private string GetQueryById(string idStr, string userEmail = "")
        {
            switch (idStr)
            {
                case "all":
                    return "";
                case "available":
                    return " Where quantity > 0";
                case "my":
                    return String.Format(" Where Id in (Select bookId From Orders Left join Users on Orders.UserId = Users.UserId Where Users.Email = '{0}')", userEmail);
                default:
                    return String.Empty;

            }
        }

        private DataTable GetTable(string sortOrder = "", int pageNum = 0, string filter = "", string userEmail = "")
        {
            int pageNumMax = 0;
            if (pageNum < 0) pageNum = 0;

            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentUser = userEmail;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) || sortOrder == "Name" ? "Name" : "Name desc";
            ViewBag.AuthorSortParm = sortOrder == "Author" ? "Author" : "Author desc";
            ViewBag.QuantitySortParm = sortOrder == "Quantity" ? "Quantity" : "Quantity desc";

            string sqlQuery = "Select * From Books";
            if (!String.IsNullOrEmpty(filter))
                sqlQuery += GetQueryById(filter, userEmail);
            if (!String.IsNullOrEmpty(sortOrder))
                sqlQuery += String.Format(" Order by {0}", sortOrder);

            DataTable table = DbManager.GetTable(sqlQuery);


            if (table.Rows.Count == 0)
            {
                pageNum = 0;
            }
            else
            {
                //double pageNumMaxDouble = (table.Rows.Count / pageSize);
                //int pageNumMax = (int)Math.Floor(pageNumMaxDouble);
                pageNumMax = table.Rows.Count / pageSize;
                if (pageNum > pageNumMax) pageNum = pageNumMax;
            }

            ViewData["PageNum"] = pageNum;
            ViewData["PageNumCurrent"] = pageNum + 1;
            ViewData["PageNumMax"] = pageNumMax + 1;
            if (table.Rows.Count > 0)
                table = table.AsEnumerable().Skip(pageSize * pageNum).Take(pageSize).CopyToDataTable();
            return table;
        }
        public ActionResult Index(string sortOrder = "", int pageNum = 0, string filter = "", string userEmail = "")
        {
            return View(GetTable(sortOrder, pageNum, filter, userEmail));
        }

        public ActionResult IndexAdmin(string sortOrder = "", int pageNum = 0, string filter = "", string userEmail = "")
        {
            ViewBag.CurrentUser = userEmail;
            return View(GetTable(sortOrder, pageNum, filter, userEmail));
        }

        public ActionResult IndexUser(string sortOrder = "", int pageNum = 0, string filter = "", string userEmail = "")
        {
            return View(GetTable(sortOrder, pageNum, filter, userEmail));
        }


        public ActionResult TakeBook(string sortOrder = "", int pageNum = 0, string filter = "", string userEmail = "", int bookId = 0)
        {            
            DataTable books = DbManager.GetTable(String.Format("Select * From Books Where Quantity > 0 and Id = {0}", bookId));
            DataTable users = DbManager.GetTable(String.Format("Select * From Users Where Email = '{0}'", userEmail));

            if (books.Rows.Count == 0) ViewBag.TakeBookMessage = "This book now is not available!"; ;
            if (users.Rows.Count > 0 && books.Rows.Count > 0)
            {
                int bookIdValue = (int)books.Rows[0]["Id"];
                int userIdValue = (int)users.Rows[0]["UserId"];

                DataTable orders = DbManager.GetTable(String.Format("Select * From Orders Where BookId = {0} and UserId={1}", bookIdValue, userIdValue));

                if (orders.Rows.Count == 0)
                {
                    using (SqlConnection sqlCon = new SqlConnection(strConnection))
                    {
                        string sqlQuery = "INSERT INTO Orders VALUES(@bookId, @userId, @dateTake, null)";

                        sqlCon.Open();
                        SqlCommand sqlCmd = new SqlCommand(sqlQuery, sqlCon);
                        sqlCmd.Parameters.AddWithValue("@bookId", bookIdValue);
                        sqlCmd.Parameters.AddWithValue("@userId", userIdValue);
                        sqlCmd.Parameters.AddWithValue("@dateTake", DateTime.UtcNow.Date);
                        sqlCmd.ExecuteNonQuery();
                    }

                    using (SqlConnection sqlCon = new SqlConnection(strConnection))
                    {
                        string sqlQuery = "Update Books Set quantity = quantity - 1 Where Id = " + bookIdValue;
                        sqlCon.Open();
                        SqlCommand sqlCmd = new SqlCommand(sqlQuery, sqlCon);
                        sqlCmd.ExecuteNonQuery();
                    }
                    ViewBag.TakeBookMessage = "You successfully took this book!";
                }
                else {
                    ViewBag.TakeBookMessage = "You already have this book!";
                }
            }           

            return RedirectToAction("IndexUser", "Library", new { @sortOrder = "Name", @pageNum = 0, @userEmail = userEmail });
        }

        public ActionResult Subscribe()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Subscribe(SubscribeModel model)
        {
            if (ModelState.IsValid && model.Email.Contains("@"))
            {
                string columnName = "Email";
                string sqlQuery = "Select " + columnName + " From Users";
                DataTable table = DbManager.GetTable(sqlQuery);

                List<string> listEmails = table.Rows.Cast<DataRow>()
                .Select(r => r.Field<string>(columnName))
                .Distinct()
                .ToList();

                if (listEmails.Contains(model.Email))
                {
                    if (IsAdmin(model.Email))
                    {
                        return RedirectToAction("IndexAdmin", "Library", new { @sortOrder = "Name", @pageNum = 0, @userEmail = model.Email });
                    }
                    {
                        return RedirectToAction("IndexUser", "Library", new { @sortOrder = "Name", @pageNum = 0, @userEmail = model.Email });
                    }
                }
                else
                {
                    // add user to DB
                    using (SqlConnection sqlCon = new SqlConnection(strConnection))
                    {
                        string sqlQueryInsert = "INSERT INTO Users VALUES(@email)";

                        sqlCon.Open();
                        SqlCommand sqlCmd = new SqlCommand(sqlQueryInsert, sqlCon);
                        sqlCmd.Parameters.AddWithValue("@email", model.Email);
                        sqlCmd.ExecuteNonQuery();
                    }

                    return RedirectToAction("IndexUser", "Library", new { @sortOrder = "Name", @pageNum = 0, @userEmail = model.Email });
                }
            }

            return View();
        }

        public bool IsAdmin(string email)
        {
            return email == "admin@mail.test";
        }

        // GET: Library/Create
        [HttpGet]
        public ActionResult Create()
        {
            return View(new BookModel());
        }

        // POST: Library/Create
        [HttpPost]
        public ActionResult Create(BookModel bookModel)
        {
            DataTable books = DbManager.GetTable(String.Format("Select * From Books Where Name = '{0}' and Author = '{1}'", bookModel.Name, bookModel.Author));

            if (books.Rows.Count == 0)
            {
                using (SqlConnection sqlCon = new SqlConnection(strConnection))
                {
                    string sqlQuery = "INSERT INTO Books VALUES(@name, @author, @quantity)";

                    sqlCon.Open();
                    SqlCommand sqlCmd = new SqlCommand(sqlQuery, sqlCon);
                    sqlCmd.Parameters.AddWithValue("@name", bookModel.Name);
                    sqlCmd.Parameters.AddWithValue("@author", bookModel.Author);
                    sqlCmd.Parameters.AddWithValue("@quantity", bookModel.Quantity);
                    sqlCmd.ExecuteNonQuery();
                }
                return RedirectToAction("IndexAdmin");
            }

            ViewBag.Error = "This book have already exist!";
            return View();            
        }

        // GET: Library/Edit/5
        public ActionResult Edit(int id)
        {
            BookModel bookModel = new BookModel();
            DataTable table = new DataTable();
            using (SqlConnection sqlCon = new SqlConnection(strConnection))
            {
                sqlCon.Open();
                string sqlQuery = "Select * From Books Where Id = @id";
                SqlDataAdapter sqlDa = new SqlDataAdapter(sqlQuery, sqlCon);
                sqlDa.SelectCommand.Parameters.AddWithValue("@id", id);
                sqlDa.Fill(table);
            }
            if (table.Rows.Count == 1)
            {
                bookModel.Id = Convert.ToInt32(table.Rows[0][0]);
                bookModel.Name = table.Rows[0][1].ToString();
                bookModel.Author = table.Rows[0][2].ToString();
                bookModel.Quantity = Convert.ToInt32(table.Rows[0][3]);
                return View(bookModel);
            }
            else return RedirectToAction("IndexAdmin");
        }

        // POST: Library/Edit/5

        [HttpPost]
        public ActionResult Edit(BookModel bookModel)
        {
            using (SqlConnection sqlCon = new SqlConnection(strConnection))
            {
                string sqlQuery = "Update Books Set Name = @name, Author = @author, Quantity = @quantity Where Id =@id";

                sqlCon.Open();
                SqlCommand sqlCmd = new SqlCommand(sqlQuery, sqlCon);
                sqlCmd.Parameters.AddWithValue("@id", bookModel.Id);
                sqlCmd.Parameters.AddWithValue("@name", bookModel.Name);
                sqlCmd.Parameters.AddWithValue("@author", bookModel.Author);
                sqlCmd.Parameters.AddWithValue("@quantity", bookModel.Quantity);
                sqlCmd.ExecuteNonQuery();
            }

            return RedirectToAction("IndexAdmin");
        }

        // GET: Library/Delete/5
        public ActionResult Delete(int id)
        {
            using (SqlConnection sqlCon = new SqlConnection(strConnection))
            {
                string sqlQuery = "Delete From Books Where Id = @id";
                sqlCon.Open();
                SqlCommand sqlCmd = new SqlCommand(sqlQuery, sqlCon);
                sqlCmd.Parameters.AddWithValue("@id", id);
                sqlCmd.ExecuteNonQuery();
            }
            return RedirectToAction("IndexAdmin");
        }


    }
}
