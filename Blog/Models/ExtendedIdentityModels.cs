using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Blog.Models
{
    public class ExtendedIdentityModels : RegisterViewModel
    {
        public HttpPostedFileBase UserProfilePicture { get; set; }
        public string City { get; set; }
        public string DateOfBirth { get; set; }
        public string ProfilePicture { get; set; }
    }
}