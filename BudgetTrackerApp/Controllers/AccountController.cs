using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using BudgetTrackerApp.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Web.Security;
using System.Collections.Generic;

namespace BudgetTrackerApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private MyBudgetTrackerAppEntities db = new MyBudgetTrackerAppEntities();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = "home/Dashboard";
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    var userId = SignInManager.AuthenticationManager.AuthenticationResponseGrant.Identity.GetUserId();
                    HttpCookie cookie = new HttpCookie("BudgetId");
                    cookie.Value = db.AccountBudgets.Single(ab => ab.UserId == userId && ab.IsOwner == true).BudgetId.ToString();
                    Response.Cookies.Add(cookie);
                    var user = UserManager.FindById(userId);
                    user.LastOnlineDate = DateTime.Now;
                    UserManager.Update(user);
                    return RedirectToAction("Dashboard", "Home");
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                string hashedSecurityPassword = UserManager.PasswordHasher.HashPassword(model.SecurityQuestionAnswer);
                var user = new ApplicationUser {
                    UserName = model.Username,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    SecurityQuestion = model.SecurityQuestion,
                    SecurityQuestionAnswer = hashedSecurityPassword,
                    Gender = model.Gender,
                    CreatedDate = DateTime.Now,
                    LastOnlineDate = DateTime.Now
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var roleManager = new RoleManager<Microsoft.AspNet.Identity.EntityFramework.IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
                   
                    if (!roleManager.RoleExists("User"))
                    {
                        var role = new IdentityRole();
                        role.Name = "User";
                        roleManager.Create(role);

                    }
                    if (!roleManager.RoleExists("Admin"))
                    {
                        var role = new IdentityRole();
                        role.Name = "Admin";
                        roleManager.Create(role);

                    }
                    if (user.UserName.Equals("Admin"))
                        await UserManager.AddToRoleAsync(user.Id, "Admin");
                    else
                    {
                        await UserManager.AddToRoleAsync(user.Id, "User");
                        // Add entries into database for new user
                        Budget newBudget = new Budget();
                        newBudget.CreatedDate = DateTime.Now;
                        db.Budgets.Add(newBudget);
                        db.SaveChanges();
                        AccountBudget newAccountBudget = new AccountBudget();
                        newAccountBudget.BudgetId = newBudget.BudgetId;
                        newAccountBudget.IsOwner = true;
                        newAccountBudget.UserId = user.Id;
                        newAccountBudget.CreatedDate = DateTime.Now;
                        newAccountBudget.IsAccepted = true;
                        db.AccountBudgets.Add(newAccountBudget);
                        Category newCategory = new Category();
                        newCategory.BudgetId = newBudget.BudgetId;
                        newCategory.Name = "Food and Groceries";
                        db.Categories.Add(newCategory);
                        Category newCategory2 = new Category();
                        newCategory2.BudgetId = newBudget.BudgetId;
                        newCategory2.Name = "Entertainment";
                        db.Categories.Add(newCategory2);
                        Category newCategory3 = new Category();
                        newCategory3.BudgetId = newBudget.BudgetId;
                        newCategory3.Name = "Utilities";
                        db.Categories.Add(newCategory3);
                        Category newCategory4 = new Category();
                        newCategory4.BudgetId = newBudget.BudgetId;
                        newCategory4.Name = "Personal Care";
                        db.Categories.Add(newCategory4);
                        db.SaveChanges();
                        HttpCookie cookie = new HttpCookie("BudgetId");
                        cookie.Value = newBudget.BudgetId.ToString();
                        Response.Cookies.Add(cookie);
                    }
                    await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);
                    
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Dashboard", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model, string Username)
        {
            if (String.IsNullOrEmpty(Username))
                return RedirectToAction("Login", "Account");
            var user = UserManager.FindByNameAsync(Username);
            if (user.Result != null)
            {
                model.Username = Username;
                model.SecurityQuestion = user.Result.SecurityQuestion;
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }
                else if (user.SecurityQuestionAnswer.Equals(model.SecurityAnswer))
                {
                    var generatedToken = UserManager.GeneratePasswordResetToken(user.Id);
                    return RedirectToAction("ResetPassword", "Account", new { username = model.Username, token = generatedToken });
                }
               return RedirectToAction("Login", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(ResetPasswordViewModel model, string username, string token)
        {
            model.Username = username;
            model.Code = token;
            return View(model);
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        //
        // GET: /Account/Settings
        public ActionResult Settings()
        {
            var viewModel = new SettingsViewModel();
            if (checkBudgetId())
            {
                var userId = User.Identity.GetUserId();
                var budgetId = db.AccountBudgets.SingleOrDefault(ab => ab.UserId == userId && ab.IsOwner == true).BudgetId;
                var budgetList = new List<SelectListItem>();
                db.AccountBudgets.Where(ab => ab.UserId == userId && (ab.IsOwner || ab.IsAccepted)).ToList().ForEach(data => {
                    budgetList.Add(new SelectListItem
                    {
                        Value = data.BudgetId.ToString(),
                        Text = UserManager.FindById(db.AccountBudgets.Single(x => x.BudgetId == data.BudgetId && x.IsOwner == true).UserId).UserName
                    });
                });
                viewModel.MyBudgetsList = budgetList;

                var pendingList = new List<SelectListItem>();
                db.AccountBudgets.Where(ab => ab.UserId == userId && !ab.IsOwner && !ab.IsAccepted).ToList().ForEach(data => {
                    pendingList.Add(new SelectListItem
                    {
                        Value = data.BudgetId.ToString(),
                        Text = UserManager.FindById(db.AccountBudgets.Single(x => x.BudgetId == data.BudgetId && x.IsOwner == true).UserId).UserName
                    });
                });
                viewModel.PendingBudgetsList = pendingList;

                var sharedList = new List<SelectListItem>();
                var myBudgetId = db.AccountBudgets.Single(ab => ab.UserId == userId && ab.IsOwner).BudgetId;
                db.AccountBudgets.Where(ab => ab.BudgetId == myBudgetId && !ab.IsOwner).ToList().ForEach(data => {
                    sharedList.Add(new SelectListItem
                    {
                        Value = data.BudgetId.ToString(),
                        Text = UserManager.FindById(data.UserId).UserName
                    });
                });
                viewModel.SharedBudgetUserList = sharedList;
            }
            return View(viewModel);
        }

        // POST: CreateSharedBudget
        [HttpPost]
        public ActionResult CreateSharedBudget(string username)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var newSharedBudget = new AccountBudget();
                var sharedUserId = UserManager.FindByName(username)?.Id;
                if (sharedUserId != null) {
                    var userId = User.Identity.GetUserId();
                    var budgetId = db.AccountBudgets.SingleOrDefault(ab => ab.UserId == userId && ab.IsOwner == true).BudgetId;
                    newSharedBudget.UserId = sharedUserId;
                    newSharedBudget.BudgetId = budgetId;
                    newSharedBudget.CreatedDate = DateTime.Now;
                    newSharedBudget.IsOwner = false;
                    newSharedBudget.IsAccepted = false;
                    db.AccountBudgets.Add(newSharedBudget);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Settings");
        }

        // POST: DeleteSharedBudget
        [HttpPost]
        public ActionResult DeleteSharedBudget(int budgetId, string username)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var sharedUserId = UserManager.FindByName(username)?.Id;
                var userId = User.Identity.GetUserId();
                var userBudgetId = db.AccountBudgets.SingleOrDefault(ab => ab.UserId == userId && ab.IsOwner == true).BudgetId;
                if (sharedUserId != null && userBudgetId == budgetId)
                {
                    var accountBudget = db.AccountBudgets.SingleOrDefault(ab => ab.BudgetId == userBudgetId && ab.UserId == sharedUserId);
                    db.AccountBudgets.Remove(accountBudget);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Settings");
        }

        // POST: AcceptSharedBudget
        [HttpPost]
        public ActionResult AcceptSharedBudget(int budgetId)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var userId = User.Identity.GetUserId();
                var accountBudget = db.AccountBudgets.SingleOrDefault(ab => ab.BudgetId == budgetId && ab.UserId == userId);
                accountBudget.IsAccepted = true;
                db.SaveChanges();
            }
            return RedirectToAction("Settings");
        }

        // POST: DeclineSharedBudget
        [HttpPost]
        public ActionResult DeclineSharedBudget(int budgetId)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var userId = User.Identity.GetUserId();
                var accountBudget = db.AccountBudgets.SingleOrDefault(ab => ab.BudgetId == budgetId && ab.UserId == userId && ab.IsOwner == false);
                db.AccountBudgets.Remove(accountBudget);
                db.SaveChanges();
            }
            return RedirectToAction("Settings");
        }

        // POST: ChangeSharedBudget
        [HttpPost]
        public ActionResult ChangeSharedBudget(int budgetId)
        {
            if (ModelState.IsValid)
            {
                var userId = User.Identity.GetUserId();
                var accountBudget = db.AccountBudgets.SingleOrDefault(ab => ab.BudgetId == budgetId && ab.UserId == userId);
                HttpCookie cookie = new HttpCookie("BudgetId");
                cookie.Value = accountBudget.BudgetId.ToString();
                Response.Cookies.Add(cookie);
                HttpCookie cookie2 = new HttpCookie("BudgetUsername");
                var budgetOwner = db.AccountBudgets.SingleOrDefault(ab => ab.BudgetId == budgetId && ab.IsOwner == true).UserId;
                cookie2.Value = UserManager.FindById(budgetOwner).UserName;
                Response.Cookies.Add(cookie);
            }
            return RedirectToAction("Settings");
        }

        // POST: DeleteMySharedBudgetModal
        [HttpPost]
        public ActionResult DeleteMySharedBudgetModal(int budgetId)
        {
            if (ModelState.IsValid)
            {
                var userId = User.Identity.GetUserId();
                var accountBudget = db.AccountBudgets.SingleOrDefault(ab => ab.BudgetId == budgetId && ab.UserId == userId && ab.IsOwner == false);
                db.AccountBudgets.Remove(accountBudget);
                db.SaveChanges();
            }
            return RedirectToAction("Settings");
        }

        // Checks if user should have access to this budgetId
        private bool checkBudgetId()
        {
            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            var userId = User.Identity.GetUserId();
            var accountBudget = db.AccountBudgets.FirstOrDefault(ab => ab.BudgetId == budgetId && ab.UserId == userId);
            return accountBudget != null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}