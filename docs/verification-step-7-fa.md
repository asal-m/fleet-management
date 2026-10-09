# قدم هفتم؛ جداسازی محیط‌ها و اجرای مستقل میزبان (M7/m11)

تاریخ: ۲۰۲۶-۱۰-۰۹. محدوده: workflow توسعه؛ قواعد کسب‌وکار، framework source و
قراردادهای REST/gRPC تغییر نکردند. معیار پایان محلی: نصب تازه، اجرای هم‌زمان
محیط کامل و میزبان، restart با حفظ credentials، suite کامل بدون skip و حفظ منابع قبلی.

## تغییرها و دلیل آن‌ها

1. [Compose وابستگی‌ها](../docker-compose.dependencies.yml) پروژهٔ مستقل
   `fleetmanagement-dependencies` و volumeهای دیتابیس/تنظیمات/کلید مستقل دارد.
   PostgreSQL روی 56432، Redis روی 57379 و fixture هویت روی 56480 هستند؛
   محیط کامل همچنان 55432/56379/55480 و REST/gRPC روی 8080/8081 را دارد.
   این تغییر تداخل نام/پورت/volume را رفع می‌کند؛ دادهٔ قدیمی منتقل یا reset نمی‌شود.
2. سرویس‌ها با `extends` از تنظیمات محیط کامل استفاده می‌کنند؛ bootstrap، uid،
   فایل‌های خصوصی 0600 و محدودیت runtime دوباره پیاده‌سازی نشدند. Redis persistence
   ندارد؛ `/data` در مسیر مستقل tmpfs است تا volume بی‌نام unused بعد از down باقی نماند.
3. [ابزار LocalDevelopment](../tools/LocalDevelopment/Program.cs) با .NET 10 و
   ProcessStartInfo.ArgumentList اجرا می‌شود؛ shell، مسیر Windows یا EF CLI لازم ندارد.
   تنظیمات Docker را فقط در حافظه می‌خواند و environment فرزند را می‌سازد؛ User Secrets
   یا credential file داخل checkout تغییر نمی‌کند. واردکردن credentials نصب دیگری
   به محیط تازه از طریق متغیرهای FLEET قدیمی نیز در این ابزار غیرفعال است.
4. API میزبان روی loopback 8180/8181 اجرا می‌شود. Migration با owner و runtime
   با حساب محدود اجرا می‌شوند؛ OIDC discovery، RSA/JWT، audience و نقش‌ها حفظ شدند.
   appsettings.Development.json لازم نیست: ابزار جایگزین تنظیمات Development را
   صریح تأمین می‌کند. Production همچنان تنظیمات خارجی و OIDC واقعی می‌خواهد.
5. modeهای `run`، `verify` و `down` اضافه شدند. verify readiness، smoke واقعی socket
   و suite کامل را اجرا و TRX را سخت‌گیر بررسی می‌کند؛ سپس host را متوقف می‌کند.
   down داده/تنظیمات/کلید را نگه می‌دارد. verify رکوردهای ساختگی و Audit در دیتابیس
   توسعه باقی می‌گذارد؛ روی نصب اختصاصی بررسی شد.
6. پروژه‌ها قبل از شروع host build می‌شوند؛ smoke/test با `--no-build` اجرا می‌شوند
   تا Windows DLLهای در حال استفاده را دوباره کپی نکند. محیط Testing مستقل است و
   HTTPS metadata فعال دارد؛ تنظیم HTTP مخصوص Development به fixture منتقل نمی‌شود.
7. Start-LocalDependencies.ps1 فقط wrapper اختیاری ابزار است؛ PostgreSqlOnly برای
   workflow کامل صریحاً رد می‌شود. پیام import نصب قدیمی نیز به restore credentials
   اصلی اشاره می‌کند؛ ساخت رمز تازه برای volume قدیمی پیشنهاد نمی‌شود.
8. README و [راهنمای محلی](local-development.md) دو مسیر، تغییر پورت و نگهداری دادهٔ
   قدیمی را توضیح می‌دهند. job جداگانهٔ Ubuntu برای host/smoke/suite و artifactها در
   [CI](../.github/workflows/verify.yml) آماده شد؛ اجرای واقعی آن هنوز مشاهده نشده است.

## شواهد نسخهٔ نهایی

دو پروژهٔ موقت با suffix `237d2083d1284bbfa3fd2284668825b8` و volumeهای خالی
ساخته شدند. محیط کامل از imageهای محلی قدم سوم استفاده کرد؛ bootstrap/identity
محیط مستقل با `up --build` و cache موجود ساخته شدند. API روی میزبان از source
فعلی build شد. هیچ‌یک از این شواهد، build سرد image جدید محصول نیست.

| اجرای نهایی | Domain | Application | Integration | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|---:|---:|
| نصب تازه؛ 8cdef2bba2274355a7cf65b85b3156c5 | 91 | 44 | 37 | **172** | 0 | 0 |
| پس از down/up؛ dd49e84724d94c71ace9043cf14e01bd | 91 | 44 | 37 | **172** | 0 | 0 |

TRX خام در `.scratch/local-development/<run-id>/results` نگهداری شد؛ علاوه بر
اعتبارسنج داخل ابزار، VerifyTests مستقلاً هر دو مجموعه را کنترل کرد. تعریف و
نتیجهٔ یکتای تمام تست‌ها، counters و هر سه assembly کامل و Passed بودند.

هر دو بار readiness و migration خودکار، چهار query واقعی gRPC، چرخهٔ مأموریت REST،
رد REST روی listener gRPC، رد درخواست ناشناس، رد تعمیر خودرو رزروشده و Audit موفق/ردشده
پاس شدند. SQL واقعی تأیید کرد runtime روی Audit فقط SELECT/INSERT دارد و
UPDATE/DELETE/TRUNCATE ندارد. نام پروژهٔ کامل و پورت تکراری پیش از startup رد شدند.

دو محیط هم‌زمان روی پورت‌های پیش‌فرض متفاوت Healthy بودند. volumeها اشتراک نداشتند
و تنظیمات/credentials آن‌ها متفاوت بود؛ متن secret فقط در حافظه مقایسه شد و چاپ نشد.
در چرخهٔ پیش از اصلاح tmpfs نیز JWT واقعی هر محیط در محیط دیگر با 401 رد شد؛
تنظیمات هویت در اصلاح tmpfs تغییر نکردند.

پس از down معمولی محیط مستقل، up مجدد همان دیتابیس و تنظیمات را استفاده کرد؛
محتوای تنظیمات قبل/بعد دقیقاً برابر بود. تنظیمات محیط کامل هم تغییر نکرد و
readiness آن پس از کل چرخه همچنان Healthy بود. mount واقعی Redis تأیید کرد
`/data` tmpfs و تنها volume آن mount فقط‌خواندنی تنظیمات است.

## مشکلات پیدا‌شده حین بررسی و اصلاحشان

- نخستین اسکریپت proof با redirection همهٔ streamها در PowerShell، پیام عادی stderr
  Docker را error تلقی کرد. این روش ثبت خروجی کنار گذاشته شد و منابع همان تلاش
  پاک شدند؛ این شکست شاهد عبور تست‌ها نبود.
- تلاش بعدی به قفل DLLهای host هنگام build SmokeClient خورد. ترتیب build قبل از
  شروع API اصلاح شد؛ suite در آن تلاش اجرا نشده بود.
- اجرای `56fc079ff10c4fd188a9632e0e7efa37` تعداد **155 Passed، 17 Failed و صفر
  Skipped** داشت. علت، انتقال RequireHttpsMetadata=false از Development به Testing
  بود. environment تست جدا و HTTPS فعال شد؛ assertion یا محافظ امنیتی تغییر نکرد.
- دو اجرای e5e1a9805710415e82425a3eaa7c9e25 و 556d52fd24024a478564b3b390f083c7
  هرکدام 172 تست را پاس کردند، اما کنترل cleanup یک volume بی‌نام Redis را پیدا کرد.
  آن volume تازه، خالی، بدون مصرف‌کننده و خارج از snapshot قبلی بررسی و فقط همان
  مورد پاک شد. tmpfs مشکل را رفع کرد؛ چرخهٔ نهایی بالا cleanup کامل داشت.

## پاک‌سازی و محدودیت‌ها

چرخهٔ نهایی exit code صفر داشت. تمام container/network/volume و image tagهای
اختصاصی تلاش‌های این قدم پاک شدند؛ ID هر ۱۶ container و نام هر ۱۷ volume اصلی
با snapshot قبلی برابر ماند. builderهای اصلی و cache مشترک حفظ شدند؛ cache همچنان
۱۴۶ رکورد / 6.908 GB داشت. prune سراسری انجام نشد؛ فضای آزاد C: حدود 14.16 GB بود.
سرویس‌های اصلی توسعه مانند پیش از این قدم متوقف‌اند.

build ابزار/solution/SmokeClient صفر warning/error داشت؛ runtime همچنان warningهای
شناخته‌شدهٔ scanning ماژول‌های بدون entity و HTTP/2 بدون TLS روی listener REST
را دارد. probe اولیهٔ EF روی جدول history هنوز ساخته‌نشده نیز قبل از migration در
log خطا ثبت کرد؛ startup/migration/readiness و suite نهایی موفق بودند. ادعای log
کاملاً بدون warning/error نمی‌شود.

M7 و مسیر محلی m11 روی Windows بررسی شدند. طراحی بدون shell برای Linux/macOS
آماده است؛ اجرای native آن‌ها و GitHub CI هنوز شاهد ندارند. Ctrl+C در mode run
طراحی شده ولی آزمون تعاملی جدا انجام نشد؛ توقف host در finally verify مشاهده شد.
M1/build سرد، D16، M6 و سایر موارد باز پلن با این قدم بسته نمی‌شوند. فایل‌ها
برای مرور محلی آماده می‌شوند؛ commit/push انجام نشده است.

کنترل بستهٔ تحویل: ۴۹ فایل Markdown و ۱۵۰ لینک محلی، بدون مقصد مفقود؛
`git diff --cached --check` و formatter check فایل جدید موفق بودند. فایل‌های
این قدم staged هستند؛ Postmanهای قبلی untracked و بدون تغییر باقی ماندند.

فرمان استفاده از ریشهٔ FleetManagement:

```sh
dotnet run --project tools/LocalDevelopment --configuration Release -- run
```
