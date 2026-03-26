using Coepd.Web.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Coepd.Web.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly CoepdDbContext _db = new CoepdDbContext();

        [HttpGet]
        public ActionResult CityDistribution()
        {
            return Json(new
            {
                labels = new[] { "Hyderabad", "Bangalore", "Chennai", "Pune", "Mumbai" },
                data = new[] { 38, 22, 14, 10, 8 }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ExperienceDistribution()
        {
            return Json(new
            {
                labels = new[] { "0+ years", "1+ years", "3+ years", "5+ years" },
                data = new[] { 42, 28, 18, 12 }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult TopIndustries()
        {
            return Json(new
            {
                labels = new[] { "Banking", "Healthcare", "E-Commerce", "Retail", "Telecom" },
                data = new[] { 31, 24, 19, 14, 9 }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult LocationTrends() => CityDistribution();

        [HttpGet]
        public ActionResult ExperienceTrends() => ExperienceDistribution();

        [HttpGet]
        public ActionResult DomainTrends() => TopIndustries();
    }
}
