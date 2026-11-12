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
