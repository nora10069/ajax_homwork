using AjaxDemo.Models;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Drawing;
using AjaxDemo.Models.DTO;
//using AspNetCore;


namespace AjaxDemo.Controllers
{
    public class ApiController : Controller
    {
        private readonly mydbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly PasswordService _passwordService;
        public ApiController(mydbContext context, IWebHostEnvironment webHostEnvironment)
        {            
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _passwordService = new PasswordService();
        }

        public IActionResult Index()
        {
            //暫停五秒
            //System.Threading.Thread.Sleep(5000);

            string content = "Ajax, 您好";
            return Content(content,"text/plain",System.Text.Encoding.UTF8);
        }

        public IActionResult Cities() {
           var cities = _context.Addresses.Select(a=>a.City).Distinct();
           return Json(cities);
        }


        public IActionResult Categories()
        {
       
            var categories = _context.Categories .ToList(); 

            return Json(categories);

        }



        [HttpPost]
        public IActionResult CheckAccount_Action([FromBody] string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("帳號不要空值");
            }

            var member = _context.Members.FirstOrDefault(m => m.Name == name);

            if (member != null)
            {
                var msg1 = "帳號已存在";
                //return Json(msg1);
                return BadRequest(msg1);
            }
            var msg2 = "帳號可使用";
            return Json(msg2);
        }


        //todo 根據 city 讀出鄉鎮區(site_id)的資料

        //todo 根據 site_id 讀出路名(road) 

        //http://...../api/avatar/3
        public IActionResult Avatar(int id=1) {
            var member = _context.Members.Find(id);
            if (member != null)
            {
                byte[] img = member.FileData;
                return File(img, "image/jpeg");
            }
            return NotFound();

        }

        public class PasswordCheckModel
        {
            public string PWD { get; set; }
            public string PWD_CHECK { get; set; }
        }



        [HttpPost]
        public IActionResult Check_PWD([FromBody] PasswordCheckModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.PWD) || string.IsNullOrEmpty(model.PWD_CHECK))
            {
                return BadRequest("密碼不該為空值");
            }

            if (model.PWD != model.PWD_CHECK)
            {
                return BadRequest("密碼驗證不相符");
            }

            return Ok();
        }


        //public IActionResult Register(string userName, string userEmail, int userAge = 20) {
        public IActionResult Register(UserDTO _user)
            {
             if (string.IsNullOrEmpty(_user.userName))
            {
                _user.userName = "Guest";
            }
           
            string info = $"{_user.userPhoto.FileName}-{_user.userPhoto.Length}-{_user.userPhoto.ContentType}";


            //實際路徑 C:\Shared\012_Ajax\workspace\AjaxDemo\wwwroot\uploads\abc.jpg
            //string strPath = @"C:\Shared\012_Ajax\workspace\AjaxDemo\wwwroot\uploads\abc.jpg";
            //string strPath = _webHostEnvironment.WebRootPath;
            //C:\Shared\012_Ajax\workspace\AjaxDemo\wwwroot
            //string strPath = _webHostEnvironment.ContentRootPath;
            //C:\Shared\012_Ajax\workspace\AjaxDemo


            //檔案上傳
            string strPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", _user.userPhoto.FileName);

            using (var fileStream = new FileStream(strPath, FileMode.Create))
            {
                _user.userPhoto.CopyTo(fileStream);
            }

            //寫進資料庫
            Member member = new Member();
            member.Name = _user.userName;
            member.Email = _user.userEmail;
            member.Age = _user.userAge;
            member.FileName = _user.userPhoto.FileName;

            // 生成鹽值
            string salt = _passwordService.GenerateSalt();

            // 生成密碼哈希
            string hashedPassword = _passwordService.HashPassword(_user.userPassword, salt);

            member.Password = hashedPassword;

            member.Salt= salt;
;
            //將上傳的檔案轉成二進位
            byte[] imgByte = null;
            using(var memorySteam = new MemoryStream())
            {
                _user.userPhoto.CopyTo(memorySteam);
                imgByte = memorySteam.ToArray();

            }
            member.FileData = imgByte;

            _context.Members.Add(member);
            _context.SaveChanges();


            return Content(strPath);
        }

       [HttpPost]
        public IActionResult Spots([FromBody]SearchDTO _searchDTO)
        {
            //根據分類編號讀取相關景點
            var spots = _searchDTO.categoryId == 0 ? _context.SpotImagesSpots : _context.SpotImagesSpots.Where(s=>s.CategoryId == _searchDTO.categoryId);

            //關鍵字搜尋
            if (!string.IsNullOrEmpty(_searchDTO.keyword))
            {
                spots = spots.Where(s => s.SpotTitle.Contains(_searchDTO.keyword) || s.SpotDescription.Contains(_searchDTO.keyword));
            }

            //根據價錢搜尋
            //根據日期區間搜尋

            //排序
            switch (_searchDTO.sortBy)
            {
                case "spotTitle":
                    spots = _searchDTO.sortType == "asc" ? spots.OrderBy(s => s.SpotTitle) : spots.OrderByDescending(s => s.SpotTitle);
                    break;
                case "categoryId":
                    spots = _searchDTO.sortType == "asc" ? spots.OrderBy(s => s.CategoryId) : spots.OrderByDescending(s => s.CategoryId);
                    break;
                default:
                    spots = _searchDTO.sortType == "asc" ? spots.OrderBy(s => s.SpotId) : spots.OrderByDescending(s => s.SpotId);
                    break;
            }



           
            //總共有多少筆資料
            int totalCount = spots.Count();
            int pageSize = _searchDTO.pageSize;
            int page = _searchDTO.page;
            //計算總共有幾頁
            int totalPages = (int)Math.Ceiling((decimal)totalCount / pageSize);
            //分頁
            spots = spots.Skip((page-1) * pageSize).Take(pageSize);


            //設定回傳的資料
            SpotsPagingDTO pagingDTO = new SpotsPagingDTO();
            pagingDTO.TotalPages = totalPages;
            pagingDTO.SpotsResult = spots.ToList();

            return Json(pagingDTO);
        }
    }
}
