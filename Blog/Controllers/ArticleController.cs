using Blog.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Blog.Controllers
{
    public class ArticleController : Controller
    {
        private BlogDbContext _dbContext = new BlogDbContext();
        
        //
        // GET: Article
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        //
        // GET: Article/List
        public ActionResult List()
        {
            var articles = _dbContext.Articles
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .ToList();

            return View(articles);
        }

        //
        // GET: Article/Details
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var article = _dbContext.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .First();

            if (article == null)
            {
                return HttpNotFound();
            }

            return View(article);
        }

        //
        // GET: Article/Create
        [Authorize]
        public ActionResult Create()
        {
            var model = new ArticleViewModel();
            model.Categories = _dbContext.Categories.OrderBy(c => c.Name).ToList();

            return View(model);
        }

        //
        // POST: Article/Create
        [HttpPost]
        [Authorize]
        public ActionResult Create(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var authorId = _dbContext.Users
                         .Where(u => u.UserName == this.User.Identity.Name)
                         .First()
                         .Id;

                var article = new Article(authorId, model.Title, model.Content, model.CategoryId);

                this.SetArticleTags(article, model, _dbContext);

                var yourSanta = _dbContext.Users.Where(y => y.UserName == model.YourSanta || y.FullName == model.YourSanta).FirstOrDefault();

                if (yourSanta != null)
                {
                    article.SantaId = yourSanta.Id;
                    var santaRoleId = _dbContext.Roles.Where(r => r.Name == "Santa")
                        .Select(r => r.Id).FirstOrDefault();
                    yourSanta.Roles.Add(new Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole
                    {
                        RoleId = santaRoleId,
                        UserId = yourSanta.Id
                    });
                }
                else
                {
                    ModelState.AddModelError("YourSanta", "Incorrect Santa! Try again, please!");
                    model.Categories = _dbContext.Categories.OrderBy(c => c.Name).ToList();

                    return View(model);
                }


                _dbContext.Articles.Add(article);
                _dbContext.SaveChanges();
               
                return RedirectToAction("Index");

            }
            using (var database = new BlogDbContext())
            {
                model.Categories = database.Categories.OrderBy(c => c.Name).ToList();
            }
            return View(model);
        }

        public ActionResult IncorrectSanta()
        {
            return View();
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var article = _dbContext.Articles
                     .Where(a => a.Id == id).Include(a => a.Author).Include(a => a.Category).First();

            if (!IsAutorizedToEdit(article))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            ViewBag.TagString = string.Join(",", article.Tags.Select(t => t.Name));

            if (article == null)
            {
                return HttpNotFound();
            }
            return View(article);
        }
        [HttpPost]
        [ActionName("Delete")]
        [Authorize(Roles = "Admin,Santa")]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var article = _dbContext.Articles
                    .Where(a => a.Id == id).Include(a => a.Author).Include(a => a.Category).First();

            _dbContext.Articles.Remove(article);
            _dbContext.SaveChanges();
            if (article == null)
            {
                return HttpNotFound();
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,Santa")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var article = _dbContext.Articles
                   .Where(a => a.Id == id).First();
            if (article == null)
            {
                return HttpNotFound();
            }
            var model = new ArticleViewModel();
            model.Id = article.Id;
            model.Title = article.Title;
            model.Content = article.Content;
            model.CategoryId = article.CategoryId;
            model.Categories = _dbContext.Categories.OrderBy(c => c.Name).ToList();


            model.Tags = string.Join(",", article.Tags.Select(t => t.Name));

            return View(model);
        }
        [HttpPost]
        [ActionName("Edit")]
        public ActionResult Edit(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var article = _dbContext.Articles
                       .FirstOrDefault(a => a.Id == model.Id);

                article.Title = model.Title;
                article.Content = model.Content;
                article.CategoryId = model.CategoryId;
                this.SetArticleTags(article, model, _dbContext);


                _dbContext.Entry(article).State = EntityState.Modified;
                _dbContext.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(model);
        }
        private bool IsAutorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);
            bool isSanta = article.IsSanta(this.User.Identity.Name);
            

            return isAdmin || isAuthor || isSanta;
        }
        private void SetArticleTags(Article article, ArticleViewModel model, BlogDbContext db)
        {
            //Split Tags
            var tagsStrings = model.Tags.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLower()).Distinct();

            //Clear all current article tags
            article.Tags.Clear();

            //Set new aricle tags
            foreach (var tagString in tagsStrings)
            {
                //Get tag form db by its name
                Tag tag = db.Tags.FirstOrDefault(t => t.Name.Equals(tagString));

                //If the tag is null, create new tag
                if (tag == null)
                {
                    tag = new Tag() { Name = tagString };
                    db.Tags.Add(tag);
                }

                //add tag to article tags
                article.Tags.Add(tag);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            _dbContext?.Dispose();

            base.Dispose(disposing);
        }
    }
}