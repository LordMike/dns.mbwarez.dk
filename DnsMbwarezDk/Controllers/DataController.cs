using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DnsMbwarezDk.Code;
using DnsMbwarezDk.Models.Data;
using WebGrease.Css.Extensions;
using WebShared.Db;
using IpInfo = WebShared.Db.IpInfo;

namespace DnsMbwarezDk.Controllers
{
    public class DataController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            DataIndexModel model = new DataIndexModel();

            model.FilterLevelFirst = true;

            return Index(model);
        }

        [HttpPost]
        public ActionResult Index(DataIndexModel model)
        {
            ViewBag.LastUpdate = DataManager.GetLastUpdate();

            model.States = DataManager.SearchDomains(model.FilterText, model.FilterIssues, model.FilterLevelFirst, model.FilterLevelSecond, model.FilterAxfr, model.FilterNsec, model.FilterNsec3, model.FilterFtp, model.FilterRsync);

            return View(model);
        }

        public ActionResult View(string id)
        {
            ViewBag.LastUpdate = DataManager.GetLastUpdate();

            id += ".";

            TldDomain state = DataManager.GetSingleTld(id);
            if (state == null)
                return RedirectToAction("Index");

            DataViewModel model = new DataViewModel();

            model.Domain = state;

            if (model.Domain.DomainLevel > 0)
                model.ChildTlds = DataManager.GetChildsForParent(model.Domain.Domain);
            else
                model.ChildTlds = new List<string>();

            model.IpInfos = new Dictionary<string, IpInfo>();
            state.Servers.ForEach(s => model.IpInfos[s.ServerIp] = new IpInfo { Ip = s.ServerIp });

            DataManager.GetIpInfo(state.Servers.Select(s => s.ServerIp).ToList()).ForEach(s => model.IpInfos[s.Ip] = s);

            model.CombinedFeatures = model.Domain.Servers.Aggregate(new ServerFeatureSet(), (set, server) => set.Combine(server.Test.FeatureSet));

            return View(model);
        }

        public ActionResult Servers()
        {
            ViewBag.LastUpdate = DataManager.GetLastUpdate();

            DataServersModel model = new DataServersModel();

            model.Servers = DataManager.GetAllServers();

            return View(model);
        }

        public ActionResult ViewServerIp(string id)
        {
            ViewBag.LastUpdate = DataManager.GetLastUpdate();

            DataViewIpModel model = new DataViewIpModel();

            model.Tlds = DataManager.GetServersByIp(id);
            if (!model.Tlds.Any())
                return RedirectToAction("Index");

            model.ServerIp = id;
            model.IpInfo = DataManager.GetIpInfo(id);
            model.CombinedFeatureSet = model.Tlds.Aggregate(new ServerFeatureSet(), (set, server) => set.Combine(server.Test.FeatureSet));

            return View(model);
        }
    }
}