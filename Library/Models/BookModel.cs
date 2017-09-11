using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace Library.Models
{
    public class BookModel
    {
        public int Id { get; set; }

        [DisplayName("Book Name")]
        public string Name { get; set; }

        public string Author { get; set; }

        public int Quantity { get; set; }
    }
}