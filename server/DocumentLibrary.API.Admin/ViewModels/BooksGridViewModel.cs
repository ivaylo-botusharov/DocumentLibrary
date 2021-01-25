using System.Collections.Generic;
using DocumentLibrary.DTO.DTOs;

namespace DocumentLibrary.API.Admin.ViewModels
{
    public class BooksGridViewModel
    {
        public List<BookListDto> BooksList { get; set; }

        public int PagesCount { get; set; }
    }
}