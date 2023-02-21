using celebrator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;




namespace celebrator.Controllers
{
    public class PersonController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _appEnvironment;

        public PersonController(AppDbContext context, IWebHostEnvironment appEnvironment)
        {
            _context = context;         
            _appEnvironment = appEnvironment;

            _context.Database.EnsureCreated();
        }

        // GET: Person
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["BirthDateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["AgeSortParm"] = sortOrder == "age" ? "age_desc" : "age";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }


            ViewData["CurrentFilter"] = searchString;

            var persons = from p in _context.Persons
                          select p;

            if (!String.IsNullOrEmpty(searchString))
            {
                persons = persons.Where(p => p.Name.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "date":
                    persons = persons.OrderBy(s => s.BirthDate.DayOfYear);
                    break;
                case "date_desc":
                    persons = persons.OrderByDescending(s => s.BirthDate.DayOfYear);
                    break;
                case "age":
                    persons = persons.OrderBy(s => DateTime.Now.Year - s.BirthDate.Year);
                    break;
                case "age_desc":
                    persons = persons.OrderByDescending(s => DateTime.Now.Year - s.BirthDate.Year);
                    break;
                case "name_desc":
                    persons = persons.OrderByDescending(s => s.Name);
                    break;
                default:
                    persons = persons.OrderBy(s => s.Name);
                    break;
            }

            int pageSize = 8;



            return persons != null ?
                View(await PaginatedList<Person>.CreateAsync(persons.AsNoTracking(), pageNumber ?? 1, pageSize)) :
                Problem("Entity set 'AppDbContext.Persons'  is null.");


        }

        public async Task<IActionResult> UpcomingBD()
        {
            return _context.Persons != null ?
                        View(await _context.Persons.AsNoTracking().Where(x => (x.BirthDate.DayOfYear - DateTime.Now.DayOfYear) >= 0 && (x.BirthDate.DayOfYear - DateTime.Now.DayOfYear) < 30).OrderBy(x => x.BirthDate.DayOfYear).ToListAsync()) :
                        Problem("Entity set 'AppDbContext.Persons'  is null.");

        }

        public async Task<IActionResult> WhitePage()
        {
            return View();
        }


        // GET: Person/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Persons == null)
            {
                return NotFound();
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(m => m.Id == id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        // GET: Person/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Person/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,BirthDate")] Person person, IFormFile? ImageFile)
        {
            if (person.BirthDate > DateTime.Now)
            {
                ModelState.AddModelError("BirthDate", "Недопустимая дата рождения");
            }

            if (ModelState.IsValid)
            {
                string path = "/images/icons/no-image.png";

                if (ImageFile != null)
                {
                    using var inStream = ImageFile.OpenReadStream();
                    var imageInfo = Image.Identify(inStream, out var format);
                    inStream.Position = 0;

                    if (imageInfo != null)
                    {
                        using var outStream = new MemoryStream();
                        using (var image = Image.Load(inStream, out format))
                        {
                            image.Mutate(x => x.Resize(300, 0));
                            image.SaveAsPng(outStream);
                            outStream.Position = 0;
                        }

                        path = "/images/photo/" + Guid.NewGuid().ToString() + "_" + ImageFile.FileName;

                        using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                        {
                            await outStream.CopyToAsync(fileStream);
                        }
                    }

                    else
                    {
                        ModelState.AddModelError("ImageSrc", "Недопустимый формат фото");
                        return View(person);
                    }

                }

                person.ImageSrc = path;

                _context.Add(person);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(person);
        }

        // GET: Person/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Persons == null)
            {
                return NotFound();
            }

            var person = await _context.Persons.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }
            return View(person);
        }

        // POST: Person/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,BirthDate")] Person person, IFormFile? ImageFile)
        {


            if (id != person.Id)
            {
                return NotFound();
            }

            if (person.BirthDate > DateTime.Now)
            {
                ModelState.AddModelError("BirthDate", "Недопустимая дата рождения");
            }

            if (ModelState.IsValid)
            {

                string path = _context.Persons.Where(x => x.Id == person.Id).Select(x => x.ImageSrc).FirstOrDefault(); ;

                if (ImageFile != null)
                {
                    using var inStream = ImageFile.OpenReadStream();
                    var imageInfo = Image.Identify(inStream, out var format);
                    inStream.Position = 0;

                    if (imageInfo != null)
                    {
                        using var outStream = new MemoryStream();
                        using (var image = Image.Load(inStream, out format))
                        {
                            image.Mutate(x => x.Resize(300, 0));
                            image.SaveAsPng(outStream);
                            outStream.Position = 0;
                        }

                        if (System.IO.File.Exists(_appEnvironment.WebRootPath + path))
                        {
                            System.IO.File.Delete(_appEnvironment.WebRootPath + path);
                        }

                        path = "/images/photo/" + Guid.NewGuid().ToString() + "_" + ImageFile.FileName;

                        using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                        {
                            await outStream.CopyToAsync(fileStream);
                        }


                    }

                    else
                    {
                        ModelState.AddModelError("ImageSrc", "Недопустимый формат фото");
                        return View(person);
                    }

                }


                person.ImageSrc = path;

                try
                {
                    _context.Update(person);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonExists(person.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(person);
        }

        // GET: Person/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Persons == null)
            {
                return NotFound();
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(m => m.Id == id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        // POST: Person/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Persons == null)
            {
                return Problem("Entity set 'AppDbContext.Persons'  is null.");
            }
            var person = await _context.Persons.FindAsync(id);
            if (person != null)
            {
                string path = _appEnvironment.WebRootPath + person.ImageSrc;

                if (System.IO.File.Exists(path) && person.ImageSrc != "/images/icons/no-image.png")
                {
                    System.IO.File.Delete(path);
                }
                _context.Persons.Remove(person);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AddTestData()
        {
            _context.Add(new Person { BirthDate = DateTime.Parse("1994-02-27 00:00:00.000"), Name = "Сергей", ImageSrc = "/images/photo/4c3db633-a32a-4dfe-812a-a1919f95bb14_2.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1997-03-01 00:00:00.000"), Name = "Антон", ImageSrc = "/images/photo/4f14d699-4f5a-442e-901f-c72924955b93_4.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1991-07-29 00:00:00.000"), Name = "Александр", ImageSrc = "/images/photo/4c3db633-a32a-4dfe-812a-a1919f95bb14_2.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1990-06-29 00:00:00.000"), Name = "Виктор", ImageSrc = "/images/photo/4c3db633-a32a-4dfe-812a-a1919f95bb14_2.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1994-01-29 00:00:00.000"), Name = "Дарья", ImageSrc = "/images/photo/6e92d9c1-1fc8-4548-a421-0e70adcf8be6_5.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1995-02-26 00:00:00.000"), Name = "Анатолий", ImageSrc = "/images/photo/6f7d2e7b-69b9-4354-8138-b3b856d7f867_6.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1995-02-20 00:00:00.000"), Name = "Давид", ImageSrc = "/images/photo/9ae9bdb7-a164-471c-8286-69c497237b0e_1.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("2004-03-29 00:00:00.000"), Name = "Николай", ImageSrc = "/images/photo/9e5ff4e0-d933-49d7-a9cd-41ccb0a7fad4_0.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1994-04-29 00:00:00.000"), Name = "Руслан", ImageSrc = "/images/photo/a19b93f1-2e07-4e1b-83ce-7a383aa828f5_3.PNG" });
            _context.Add(new Person { BirthDate = DateTime.Parse("1994-05-29 00:00:00.000"), Name = "Кирилл", ImageSrc = "/images/photo/4c3db633-a32a-4dfe-812a-a1919f95bb14_2.PNG" });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool PersonExists(int id)
        {
            return (_context.Persons?.Any(e => e.Id == id)).GetValueOrDefault();
        }

    }
}
