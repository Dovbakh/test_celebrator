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

            if(!String.IsNullOrEmpty(searchString))
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

                if (ImageFile != null) // если файл загрузили
                {
                    using var inStream = ImageFile.OpenReadStream();
                    var imageInfo = Image.Identify(inStream, out var format);
                    inStream.Position = 0; // "отматываем" стрим в начало

                    if (imageInfo != null) // если этот файл - картинка
                    {
                        using var outStream = new MemoryStream();
                        using (var image = Image.Load(inStream, out format)) // загружаем картинку
                        {
                            image.Mutate(x => x.Resize(300, 0)); // меняем размер картинки
                            image.SaveAsPng(outStream);
                            outStream.Position = 0;
                        }

                        path = "/images/photo/" + Guid.NewGuid().ToString() + "_" + ImageFile.FileName;

                        using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create)) // копируем картинку в папку на сервере
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

                if (ImageFile != null) // если файл загрузили
                {
                    using var inStream = ImageFile.OpenReadStream();
                    var imageInfo = Image.Identify(inStream, out var format);
                    inStream.Position = 0; // "отматываем" стрим в начало

                    if (imageInfo != null) // если этот файл - картинка
                    {
                        using var outStream = new MemoryStream();
                        using (var image = Image.Load(inStream, out format)) // загружаем картинку
                        {
                            image.Mutate(x => x.Resize(300, 0)); // меняем размер картинки
                            image.SaveAsPng(outStream);
                            outStream.Position = 0;
                        }

                        if (System.IO.File.Exists(_appEnvironment.WebRootPath + path)) // удалить предыдущую картинку
                        {
                            System.IO.File.Delete(_appEnvironment.WebRootPath + path);
                        }

                        path = "/images/photo/" + Guid.NewGuid().ToString() + "_" + ImageFile.FileName; // переназначаем путь для новой картинки

                        using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create)) // копируем картинку в папку на сервере
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

        private bool PersonExists(int id)
        {
            return (_context.Persons?.Any(e => e.Id == id)).GetValueOrDefault();
        }

    }
}
