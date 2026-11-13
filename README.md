# CMS Backend

بک‌اند ASP.NET Core (net10) با EF Core و SQL Server. شامل احراز هویت JWT با refresh token، مجوزهای نقش‌محور، مدیریت محتوا (صفحه، مقاله، دسته‌بندی، تگ) و فروشگاه (محصول، سبد خرید، سفارش).

## پیش‌نیازها

- .NET SDK 10
- SQL Server (یا LocalDB)

## تنظیمات

مقادیر حساس را در فایل‌های appsettings قرار ندهید. در توسعه از user-secrets و در سرور از متغیرهای محیطی استفاده کنید.

| کلید | توضیح |
| --- | --- |
| `Jwt:Key` | الزامی، حداقل ۳۲ کاراکتر |
| `Seed:AdminUserName` | الزامی، نام کاربری ادمین اولیه |
| `Seed:AdminPassword` | الزامی، رمز ادمین اولیه (حداقل ۸ کاراکتر با حرف بزرگ، کوچک، عدد و نماد) |
| `ConnectionStrings:DefaultConnection` | رشته اتصال دیتابیس |
| `Cors:AllowedOrigins` | آدرس فرانت‌اند، آرایه‌ای از origin ها |
| `ForwardedHeaders:KnownProxies` | IP پروکسی معکوس. پشت nginx یا مشابه حتماً پر شود، وگرنه rate limit روی IP پروکسی اعمال می‌شود |
| `Auth:CookieSameSite` | پیش‌فرض `None` |
| `Database:AutoMigrate` | اجرای خودکار migration هنگام شروع. در Development برابر `true` و در بقیه محیط‌ها `false` |

## اجرای محلی

```bash
cd BackEnd
dotnet user-secrets set "Jwt:Key" "یک-کلید-تصادفی-حداقل-32-کاراکتری"
dotnet user-secrets set "Seed:AdminUserName" "admin"
dotnet user-secrets set "Seed:AdminPassword" "Admin@12345"
dotnet run --launch-profile https
```

در حالت Development، دیتابیس به‌صورت خودکار ساخته و migration ها اعمال می‌شوند و مستندات API روی `/scalar/v1` در دسترس است.

## Migration در سرور

```bash
dotnet ef database update --project BackEnd
```

یا با `Database__AutoMigrate=true` در متغیرهای محیطی.

## کنترل هم‌زمانی (RowVersion)

برای پست، صفحه و محصول، هر درخواست ویرایش، حذف و تغییر وضعیت باید `RowVersion` آخرین خوانده‌شده را بفرستد (در بدنه یا در هدر `If-Match`). عدم تطابق، پاسخ 409 برمی‌گرداند. مقدار فعلی در هدر `ETag` و فیلد `rowVersion` پاسخ‌های GET موجود است. ثبت سفارش موجودی محصول را تغییر می‌دهد و `RowVersion` را عوض می‌کند، پس ویرایشی که بعد از آن ارسال شود با نسخه‌ی قدیمی رد می‌شود و باید دوباره خوانده شود.

## کش عمومی

خروجی endpoint های عمومی ۳۰ ثانیه کش می‌شود. هر عملیات موفق ایجاد، ویرایش، حذف، انتشار یا فعال‌سازی روی پست، صفحه، دسته‌بندی، تگ، محصول و برند، کش عمومی را بلافاصله پاک می‌کند.

## کتابخانه‌ی مدیا

تصاویر داخل محتوای مقاله، صفحه و توضیحات محصول از اینجا مدیریت می‌شوند.

| متد و مسیر | مجوز | توضیح |
| --- | --- | --- |
| `GET /api/Media` | `Media.View` | لیست صفحه‌بندی‌شده، با `search` روی نام فایل و متن جایگزین |
| `GET /api/Media/{id}` | `Media.View` | جزئیات یک تصویر |
| `POST /api/Media` | `Media.Manage` | آپلود با `multipart/form-data` و فیلدهای `image` و `altText` |
| `PATCH /api/Media/{id}` | `Media.Manage` | ویرایش `altText` |
| `DELETE /api/Media/{id}` | `Media.Manage` | حذف. اگر آدرس تصویر در پست، صفحه یا توضیحات محصول باشد، با خطای ۴۰۰ رد می‌شود |
| `GET /api/Media/files/{fileName}` | عمومی | فایل اصلی، یا با `?size=thumb` تصویر کوچک |

آدرس `url` در پاسخ را مستقیم در `<img src>` محتوا قرار دهید. این آدرس نسبی است و از sanitizer عبور می‌کند. هر تصویر آپلودشده عمومی است.

نقش‌های سفارشی موجود، دو مجوز `Media.View` و `Media.Manage` را ندارند و باید از بخش نقش‌ها اضافه شوند. نقش Admin همه‌ی مجوزها را دارد.

جدول `MediaFiles` با migration با نام `AddMediaLibrary` ساخته می‌شود. این migration دستی نوشته شده و فایل Designer ندارد. برای اطمینان بعد از دریافت تغییر این دستور را اجرا کنید و مطمئن شوید migration جدیدی با محتوای خالی ساخته می‌شود:

```bash
dotnet ef migrations add VerifyModel --project BackEnd
```

اگر محتوای `Up` خالی بود، آن migration را با `dotnet ef migrations remove --project BackEnd` حذف کنید. سپس migration ها را اعمال کنید:

```bash
dotnet ef database update --project BackEnd
```

## انتشار زمان‌بندی‌شده

برای پست و صفحه می‌توان زمان انتشار آینده تعیین کرد.

| متد و مسیر | مجوز | بدنه |
| --- | --- | --- |
| `PATCH /api/Posts/{id}/schedule` | `Posts.Publish` | `{ "scheduledAt": "2026-12-01T10:00:00+03:30", "rowVersion": "..." }` |
| `PATCH /api/ContentPages/{id}/schedule` | `Pages.Publish` | همان ساختار |

- `scheduledAt` را با offset یا `Z` بفرستید. مقدار `null` زمان‌بندی را لغو می‌کند.
- زمان باید در آینده باشد، محتوا خالی نباشد و مورد قبلاً منتشر نشده باشد.
- یک سرویس پس‌زمینه هر دقیقه موارد سررسیدشده را منتشر می‌کند، `PublishedAt` را برابر زمان تعیین‌شده می‌گذارد و کش عمومی را پاک می‌کند.
- انتشار یا لغو انتشار دستی، زمان‌بندی را پاک می‌کند.
- فیلد `scheduledAt` در پاسخ لیست و جزئیات پست و صفحه موجود است و به‌صورت UTC ذخیره می‌شود.

## پیش‌نمایش پیش‌نویس

| متد و مسیر | مجوز |
| --- | --- |
| `GET /api/Posts/{id}/preview` | `Posts.View` |
| `GET /api/ContentPages/{id}/preview` | `Pages.View` |

خروجی همان ساختار endpoint عمومی را دارد، ولی برای محتوای منتشرنشده هم کار می‌کند و کش نمی‌شود. تصویر شاخص پیش‌نمایش پست از endpoint ادمین سرو می‌شود و به کوکی ورود نیاز دارد.

## سئو

`GET /sitemap.xml` و `GET /rss.xml` عمومی هستند و مثل بقیه‌ی خروجی‌های عمومی ۳۰ ثانیه کش می‌شوند. اگر `Site:BaseUrl` تنظیم نشده باشد، پاسخ ۴۰۴ است.

| کلید | توضیح |
| --- | --- |
| `Site:BaseUrl` | آدرس سایت عمومی (فرانت‌اند)، مثلاً `https://example.com` |
| `Site:Title` و `Site:Description` | عنوان و توضیح فید RSS |
| `Site:Language` | زبان فید، پیش‌فرض `fa` |
| `Site:PostPathTemplate` | مسیر پست در فرانت‌اند، پیش‌فرض `/blog/{slug}` |
| `Site:PagePathTemplate` | مسیر صفحه، پیش‌فرض `/pages/{slug}` |
| `Site:ProductPathTemplate` | مسیر محصول، پیش‌فرض `/products/{slug}` |

نقشه‌ی سایت شامل صفحه‌ی اصلی، پست‌ها و صفحات منتشرشده و محصولات فعال است و حداکثر ۵۰٬۰۰۰ آدرس دارد. موتورهای جست‌وجو انتظار دارند فایل روی همان دامنه‌ی آدرس‌ها باشد، پس فرانت‌اند یا reverse proxy باید این دو مسیر را به بک‌اند هدایت کند و در `robots.txt` خط `Sitemap: https://example.com/sitemap.xml` قرار گیرد.

## منو

منوها با کلید یکتا ساخته می‌شوند (مثلاً `header` و `footer`) و هر منو یک درخت از آیتم‌ها دارد. حداکثر عمق ۳ سطح و حداکثر ۱۰۰ آیتم است.

| متد و مسیر | مجوز | توضیح |
| --- | --- | --- |
| `GET /api/Menus` | `Menus.Manage` | لیست منوها |
| `GET /api/Menus/{id}` | `Menus.Manage` | منو با درخت آیتم‌ها |
| `POST /api/Menus` | `Menus.Manage` | ساخت منو با `key` و `name` |
| `PATCH /api/Menus/{id}` | `Menus.Manage` | ویرایش نام |
| `PUT /api/Menus/{id}/items` | `Menus.Manage` | جایگزینی کامل آیتم‌ها |
| `DELETE /api/Menus/{id}` | `Menus.Manage` | حذف منو و آیتم‌ها |
| `GET /api/PublicContent/menus/{key}` | عمومی | خروجی عمومی منو، کش‌شده |

بدنه‌ی `PUT /api/Menus/{id}/items`:

```json
{
  "items": [
    { "title": "خانه", "url": "/" },
    {
      "title": "وبلاگ",
      "url": "/blog",
      "children": [
        { "title": "اخبار", "url": "/blog/news", "openInNewTab": false }
      ]
    },
    { "title": "گیت‌هاب", "url": "https://github.com", "openInNewTab": true }
  ]
}
```

ترتیب آیتم‌ها همان ترتیب آرایه است. آدرس باید مسیر نسبی با `/` یا آدرس کامل `http` و `https` باشد و مقادیر دیگر مثل `javascript:` رد می‌شوند.

## تنظیمات سایت

| متد و مسیر | مجوز | توضیح |
| --- | --- | --- |
| `GET /api/SiteSettings` | `Settings.Manage` | خواندن تنظیمات |
| `PUT /api/SiteSettings` | `Settings.Manage` | ذخیره‌ی کامل تنظیمات |
| `GET /api/PublicContent/site-settings` | عمومی | خروجی عمومی، کش‌شده |

فیلدها: `siteName`، `tagline`، `logoUrl`، `faviconUrl`، `defaultSeoTitle`، `defaultMetaDescription`، `contactEmail`، `contactPhone`، `address`، `instagramUrl`، `telegramUrl`، `whatsappUrl`، `linkedinUrl`، `xUrl` و `footerText`. `logoUrl` و `faviconUrl` را می‌توان از `url` کتابخانه‌ی مدیا پر کرد و آن تصویر تا زمانی که در تنظیمات یا منو استفاده شده باشد قابل حذف نیست. فیلدی که ارسال نشود یا خالی باشد پاک می‌شود، پس همیشه مقدار کامل را بفرستید.

مجوزهای `Menus.Manage` و `Settings.Manage` برای نقش‌های سفارشی باید دستی اضافه شوند. نقش Admin همه‌ی مجوزها را دارد. مقدار `Site:Title` در appsettings فقط برای فید RSS استفاده می‌شود و از تنظیمات سایت جدا است.

## تاریخچه‌ی نسخه‌ها

با هر ایجاد، ویرایش (فقط وقتی عنوان، اسلاگ، محتوا، خلاصه یا فیلدهای سئو واقعاً تغییر کرده باشند) و بازگردانی مقاله یا صفحه یک نسخه ذخیره می‌شود. حداکثر ۵۰ نسخه‌ی آخر برای هر مورد نگه داشته می‌شود و با حذف مقاله یا صفحه، نسخه‌هایش هم حذف می‌شوند.

- `GET /api/Posts/{id}/revisions` و `GET /api/ContentPages/{id}/revisions`: فهرست نسخه‌ها (مجوز View)
- `GET /api/Posts/{id}/revisions/{revisionId}` و معادل آن برای صفحه‌ها: متن کامل نسخه (مجوز View)
- `POST /api/Posts/{id}/revisions/{revisionId}/restore` و معادل آن برای صفحه‌ها: بازگردانی با بدنه‌ی `{ "rowVersion": "..." }` (مجوز Manage)

بازگردانی عنوان، خلاصه، محتوا و فیلدهای سئو را برمی‌گرداند. اسلاگ، دسته‌بندی، برچسب‌ها و وضعیت انتشار دست‌نخورده می‌مانند و محتوا دوباره پاک‌سازی می‌شود.

## گزارش فعالیت

هر درخواست موفق تغییردهنده (POST، PUT، PATCH، DELETE) از کاربر واردشده‌ی هر کنترلر به‌جز Auth در جدول `AuditLogs` ثبت می‌شود: کاربر، کنترلر، اکشن، متد، مسیر، شناسه‌ی موجودیت، کد وضعیت، آی‌پی و زمان. بدنه‌ی درخواست ذخیره نمی‌شود. شکست در ثبت لاگ باعث خطای درخواست نمی‌شود.

`GET /api/AuditLogs` با مجوز `Audit.View` و فیلترهای `userId`، `controller`، `entityId`، `from`، `to`، `page` و `pageSize` (حداکثر ۲۰۰). لاگ‌های قدیمی‌تر از `Audit:RetentionDays` (پیش‌فرض ۱۸۰ روز، مقدار صفر یعنی بدون حذف) هر ۲۴ ساعت پاک می‌شوند. مجوز `Audit.View` برای نقش‌های سفارشی باید دستی اضافه شود.

## گالری و ویژگی‌های محصول

عکس اصلی محصول (`ImgPath`) به‌عنوان کاور باقی می‌ماند و هر محصول علاوه بر آن حداکثر ۱۰ تصویر گالری دارد.

- `GET /api/Products/{id}/images`: فهرست تصاویر گالری (مجوز View)
- `GET /api/Products/{id}/images/{imageId}/file?size=thumb`: فایل تصویر (مجوز View)
- `POST /api/Products/{id}/images`: آپلود با `multipart/form-data` و فیلدهای `image` و `altText` (مجوز Manage، محدودیت نرخ `upload`)
- `PATCH /api/Products/{id}/images/{imageId}`: ویرایش متن جایگزین
- `PUT /api/Products/{id}/images/order`: ترتیب جدید با `{ "imageIds": [3, 1, 2] }`؛ باید دقیقاً همه‌ی تصاویر محصول باشند
- `DELETE /api/Products/{id}/images/{imageId}`: حذف تصویر و فایل آن

ویژگی‌ها دو بخش دارند: تعریف ویژگی (مثل «رنگ» یا «جنس») و مقدار آن برای هر محصول.

- `GET/POST /api/ProductAttributes`، `PATCH/DELETE /api/ProductAttributes/{id}`: مدیریت تعریف ویژگی‌ها با `name`، `slug`، `sortOrder` و `isFilterable` (مجوزهای Products)؛ حذف ویژگی مقدارهای آن را هم حذف می‌کند
- `GET /api/Products/{id}/attributes` و `PUT /api/Products/{id}/attributes`: خواندن و جایگزینی کامل مقدارها با `{ "items": [{ "attributeId": 1, "value": "قرمز" }] }`. مقدار خالی یعنی حذف آن ویژگی؛ حداکثر ۵۰ مورد
- جزئیات محصول در پنل و سایت (`ProductDto` و `PublicProductDto`) فیلدهای `images` و `attributes` را دارد
- سایت عمومی: `GET /api/PublicProducts/products/{id}/images/{imageId}?size=thumb`، `GET /api/PublicProducts/attributes` (ویژگی‌های فیلترپذیر با مقادیر محصولات فعال) و فیلتر فهرست محصولات با `attr=attributeId:value`، مثلاً `?attr=1:قرمز&attr=1:آبی&attr=2:پنبه` (OR میان مقدارهای یک ویژگی و AND میان ویژگی‌های مختلف، حداکثر ۱۰ شرط)
