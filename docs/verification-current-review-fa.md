# شاهد اصلاحات فعلی ریویو

## بازبینی تکمیلی ۲۰۲۶-۱۰-۰۹

بررسی فایل اصلی چالش روشن کرد که بند ۶ فقط Scheduled Time را الزام می‌کند؛
مدت، زمان پایان و قواعد شروع زودتر/تأخیر در آن مشخص نشده‌اند. رزرو بازه‌ای
الزام صریح چالش نیست. M3 از مسیر مستندسازی و تستِ رزرو انحصاری فعلی پوشش داده
شده است؛ درخواست قبلی کاربر برای افزودن هر دو مدل، قابلیت اضافه و هنوز اجرا‌نشده
است. تفکیک الزام از قابلیت اضافه در [D17](decisions/D17-reservation-and-schedule-policy.md)
ثبت شده؛ پذیرش نهایی طراحی با ارزیاب است.

- M1: اجرای معمول `dotnet test` بدون وابستگی، اکنون با `FLEETTEST001` شکست روشن
  می‌دهد؛ کنترل منفی روی پروژهٔ Integration با خروجی ۱ تأیید شد.
- M7: runner پیش‌فرض از cache معمول Docker برای ساخت source فعلی استفاده می‌کند؛
  project، volumeها، پورت‌ها و نتایج هر اجرا همچنان تازه و مستقل‌اند.
  builder سرد فقط با `-- --cold-build` انتخاب می‌شود؛ جزئیات در
  [D22](decisions/D22-full-suite-runner.md) ثبت شده است.
- m9: نام‌های کامل باقی‌مانده در ثبت ماژول‌ها، منابع پیام، query سرویس gRPC و
  parser زمان به using/alias تبدیل شدند. تداخل نام ResultFailureException با
  Wolverine در اولین build مشخص و با alias صریح برطرف شد.
- `.gitattributes` پایان‌خط فایل‌های source/قرارداد/مستندات را LF تعیین می‌کند.
  ۶۵ لینک محلی در اسناد اصلی معتبرند. چهار فایل Postman JSON معتبرند و در
  متغیرهای token/secret/password آن‌ها اعتبارنامهٔ ثابت پیدا نشد؛ Newman اجرا نشده.
- اجرای نهایی میزبان با شناسهٔ `41b03734b6164c96ae7e10d6edee6cb0`،
  **۹۱ Domain + ۴۴ Application + ۴۴ Integration = ۱۷۹ موفق، صفر شکست و صفر skip**
  داشت. REST، چهار query gRPC، OIDC، مجوزهای Audit و چرخهٔ مأموریت نیز پاس شدند.
- `dotnet format whitespace --verify-no-changes` و `git diff --check` موفق بودند.
- اجرای کامل فرمان رسمی با شناسهٔ `b1b680920e134a7ab1da00fc8e614b67` نیز موفق شد:
  image محصول از source فعلی ساخته شد، Compose با volumeهای تازه بالا آمد،
  bootstrap/migration/readiness تأیید شد و **۱۷۹ تست، صفر شکست و صفر skip** پاس شد.
  TRXها در `.scratch/full-suite/b1b680920e134a7ab1da00fc8e614b67/results` هستند.
  این اجرا از cache مجاز build استفاده کرد؛ اجرای سرد یا Ubuntu CI ادعا نمی‌شود.
- منابع آزمایشی این دو اجرای تکمیلی پاک شدند؛ builder موقت اجرای سرد لغوشده
  نیز حذف شد. محیط قبلی توسعه و cache مشترک دست‌نخورده ماندند.

بخش‌های پایین‌تر شامل شواهد اجرای قبلی‌اند؛ سیاست runner در D22 جایگزین توضیح
قبلیِ builder سرد اجباری است. رزرو بازه‌ای هنوز منتظر پاسخ قواعد کسب‌وکار و دلیل
commit تجمیع‌شده هنوز منتظر توضیح واقعی کاربر است؛ تکمیل این دو ادعا نمی‌شود.

تاریخ: ۲۰۲۶-۱۰-۰۹؛ working tree روی پایهٔ `3a61cf0`، MP Core 0.9.3 و .NET 10.
این گزارش جایگزین ادعای تأیید اجرای فعلی در گزارش‌های قدم‌های قبلی است؛
گزارش‌های قبلی به‌عنوان تاریخچه حفظ شده‌اند.

## تغییرهای این بررسی

- قفل سراسری جای خود را به قفل هر Mission و Driver/Vehicle با ترتیب ثابت داد.
  ثبت منابع تازه مستقل است؛ شمارندهٔ revision فقط با تغییر availability افزایش
  می‌یابد. تست واقعی اثبات کرد قفل یک خودرو مانع خودرو/مأموریت مستقل نمی‌شود.
- خروجی enumهای محصول در REST نام‌دار شد؛ ورودی عددی قبلی همچنان پذیرفته است.
  تست‌های برابری gRPC، SmokeClient و انتظارهای دو مجموعهٔ Postman به‌روز شدند.
- بدنهٔ endpointهای async و statementهای فشرده باز شدند؛ نام‌های کامل پراستفاده
  در Program/DI به usingهای معمول تبدیل شدند. `.editorconfig` قالب ثابت می‌دهد.
- خطای unique index رزرو در commit به Conflict با هویت Vehicle/Driver تبدیل
  می‌شود. تست واقعی HTTP 409 و rollback تغییر/Audit موفق اجرا شد.
- README اکنون GET راننده، Location ثبت راننده و قرارداد enum جدید را درست
  توضیح می‌دهد. تصمیم قفل/REST در [D21](decisions/D21-resource-locks-and-readable-rest-states.md) ثبت شد.

## اجرای کامل Windows و Linux

محیط مستقل `fleetmanagement-dependencies-reviewfix20261009` با volumeهای تازه
برای این بررسی ساخته شد. پورت‌های آن 58432/58379/58480 و میزبان 8280/8281 بودند.
پورت‌های پیش‌فرض با محیط قبلی تداخل داشتند؛ تلاش اول به همین علت متوقف شد.
منابع موجود قبلی تغییر نکردند. اعتبارنامه‌ها در volume خصوصی و حافظهٔ فرزند ماندند.

فرمان Windows از ریشهٔ پروژه:

```text
dotnet run --project tools/LocalDevelopment --configuration Release -- verify --project fleetmanagement-dependencies-reviewfix20261009
```

متغیرهای FLEET_DEPENDENCIES_POSTGRES_PORT، FLEET_DEPENDENCIES_REDIS_PORT،
FLEET_DEPENDENCIES_IDENTITY_PORT، FLEET_LOCAL_REST_PORT و FLEET_LOCAL_GRPC_PORT
به پورت‌های بالا تنظیم شدند. build solution و SmokeClient صفر warning/error داشتند.
شناسهٔ نتایج: `607a4aabbff64c95959b78c7cbfb2418`.

| محیط | Domain | Application | Integration | موفق | شکست | Skip |
|---|---:|---:|---:|---:|---:|---:|
| Windows native host | 91 | 44 | 44 | **179** | 0 | 0 |
| Linux SDK container | 91 | 44 | 44 | **179** | 0 | 0 |

تمام outcomeهای TRX و شمارنده‌ها توسط VerifyTests سخت‌گیر بررسی شدند.
TRXهای Windows در `.scratch/local-development/607a4aabbff64c95959b78c7cbfb2418/results`
و Linux موفق در `.scratch/linux-current-results-success` نگهداری شدند.

suite Linux از source همین بررسی داخل SDK 10 ساخته و روی شبکهٔ Docker مستقل
به PostgreSQL/Redis متصل شد؛ source میزبان یا تست‌ها برای عبور آن تغییر نکردند.
runner موقت در `.scratch/LinuxSuite` تنظیمات خصوصی mount را در حافظه خواند و
هر دو متغیر FLEET_TEST و ConnectionStrings لازم برای راه‌اندازی MP Core را به
فرزند داد. تلاش اولیه به علت نبود ConnectionStrings راه‌اندازی، ۱۹ Integration
ناموفق و ۲۵ موفق داشت؛ Domain/Application پاس بودند. TRXهای شکست در
`.scratch/linux-current-results` حفظ شدند. این شکست با اصلاح harness تکرار شد،
نه با تضعیف assertions یا امنیت محصول.

این شاهد واقعی اجرای Linux است، اما اجرای workflow روی GitHub/Ubuntu میزبان
یا macOS نیست؛ آن محیط‌ها هنوز مشاهده نشده‌اند. فرمان کامل VerifyTests با
builder سرد اختصاصی نیز امتحان شد: پروژهٔ `fleet-tests-5856147256a8483a86cd2833b8e607c2`
در دانلود بسیار کند imageهای پایه باقی ماند و پیش از build/test با Ctrl+C متوقف
شد. builder و منابع اختصاصی آن پاک شدند؛ این تلاش شاهد موفقیت build سرد نیست.

## image فعلی، socket و observability

Dockerfile محصول با کد فعلی به image `fleet-reviewfix-application:20261009`
build شد؛ digest مشاهده‌شده `9d98e96ecd1a20af44750b31f6bb158c02632397b286537a5cea44d0dc29abb8` است.
این build از cache مجاز NuGet و image پایه استفاده کرد، نه image قدیمی محصول.

image تازه روی 8380/8381، همان دیتابیس آزمایشی و fixture هویت جدا اجرا شد.
SmokeClient socket واقعی، چهار query gRPC، نام وضعیت‌های REST، GET راننده،
چرخهٔ مأموریت، رد ناشناس، تعمیر خودروی رزروشده و Audit موفق/رد را پاس کرد.
آزمون مجوزهای runtime Audit در اجرای Windows نیز SELECT/INSERT و ممنوعیت
UPDATE/DELETE/TRUNCATE را تأیید کرد. fixture هویت موقت ابتدا به مجوز tmpfs
خورد؛ uid/gid=1654 و mode=0700 هماهنگ شد و اجرای نهایی موفق شد.

روی image تازه metric واقعی `fleet_availability_cache_requests_total` با labelهای
`cache_outcome="miss"` و `cache_outcome="hit"` از endpoint محافظت‌شدهٔ metrics
خوانده شد. trace یکتای `3f7f4f54f9e94a0e843e5a23901872b2` در collector محلی
Jaeger موجود دریافت شد: ۱۲ span از دو GET، شامل route، query، HybridCache و
PostgreSQL. فقط trace ID و نام spanها در `.scratch/observability-current-proof.json`
ذخیره شدند؛ توکن، secret و کلید cache چاپ یا ذخیره نشدند. آزمون همبستگی
Application/Domain/DB در suite نیز پاس شد. Jaeger قبلی حذف یا بازپیکربندی نشد.

کد و مستندات قالب‌بندی نهایی شدند؛ `git diff --check` با لحاظ CR انتهای خط
موفق بود. دو مجموعهٔ Postman JSON معتبرند؛ Newman در این بررسی اجرا نشده است.

## موارد باز واقعی

1. کاربر هم رزرو فعلی و هم رزرو بازه‌ای را خواسته است؛ پیشنهاد رفتار ExpectedEndTime،
   مجازبودن بازه‌های چسبیده، منع شروع زودتر و نگه‌داشتن منبع InProgress دیرکرده
   برای تعیین سیاست پرسیده شد. پاسخ آن هنوز نرسیده؛ مدل بازه‌ای پیاده نشده است.
2. طبق انتخاب کاربر، کدهای نوع/صلاحیت قابل گسترش می‌مانند؛ D20 محدودیت TRUK را
   روشن می‌کند. کاتالوگ محدود ساختگی اضافه نمی‌شود.
3. علت تجمیع commit اولیه هنوز از کاربر دریافت نشده؛ نویسندگی/تسلط شخصی از
   نتیجهٔ تست استنتاج نمی‌شود. تاریخچه بازنویسی نشده است.
4. D16 به تفسیر ارزیاب دربارهٔ storage داخلی Wolverine وابسته است؛ framework
   source، تراکنش و Audit برای پنهان‌کردن آن تغییر نکرده‌اند.
5. شمارندهٔ کش و دو query نسخه در cache hit حفظ شده‌اند؛ هزینهٔ آن در D21 روشن
   است. چرخهٔ مفهومی خواندن Contracts نیز طبق D19 توضیح داده شده، حذف نشده است.

commit/push جدید در این بررسی انجام نشده؛ فایل‌های جدید لازم باید در تحویل
اصلاح‌شده گنجانده شوند. پذیرش نهایی متعلق به ریویور است.

## پاک‌سازی نهایی

کانتینرهای مستقل برنامه/هویت و پروژهٔ dependency همین بررسی همراه سه volume
و image tagهای اختصاصی حذف شدند. چهار کانتینر فعال قبلی، از جمله PostgreSQL/Redis
محیط traceproof و Jaeger، همچنان فعال‌اند؛ builder/cache مشترک prune نشد.
TRXها و شاهد trace در workspace حفظ شدند. کنترل لینک‌های هفت سند اصلی صفر
مسیر محلی ناموجود داشت و diff whitespace check نهایی موفق بود.

## تطبیق نهایی شناسه‌های ریویو

| مورد | وضعیت فعلی |
|---|---|
| C1 | مستندات الزامی موجود؛ انتشار نسخه هنوز انجام نشده |
| C2 | سیاست تعمیر تعریف، مستند و در suite فعلی آزموده شده |
| M1 | runner رسمی و میزبان هرکدام ۱۷۹ موفق؛ اجرای بدون وابستگی FLEETTEST001 می‌دهد؛ CI میزبان/macOS هنوز مشاهده نشده |
| M2 | طبقه‌بندی 404/409/422 و gRPC آزموده شده |
| M3 | مسیر مستندسازی/تست مدل فعلی پوشش داده شده؛ بازه‌ای الزام صریح چالش نیست و درخواست اضافهٔ کاربر هنوز پیاده نشده |
| M4 | قفل هر منبع و ابطال هدفمند آزموده؛ هزینهٔ شمارنده و دو خواندن DB مستند |
| M5 | مالکیت و چرخهٔ Contracts توضیح داده شده؛ چرخهٔ مفهومی حفظ شده |
| M6 | دلیل commit بزرگ و تأیید مستقل نویسندگی باز |
| M7 | محیط‌ها جدا؛ runner با cache معمول ساده شده؛ image فعلی، migration تازه و suite کامل آزموده |
| m1 | handlerهای جدا رفتار را با delegate تعیین می‌کنند؛ رشته فقط عنوان Audit/span است |
| m2 | tracing از Domain منتقل شده و محافظ معماری پاس است |
| m3 | نگاشت واقعی وضعیت خودرو و mapper مشترک پاس است |
| m4 | validator و guard زمان در Application پاس است |
| m5 | enum رشته‌ای، GET راننده، Location و view کامل اجرا شده |
| m6 | conflict واقعی commit و rollback/409 اجرا شده |
| m7 | ماتریس lifecycle مستند و تست‌ها پاس‌اند |
| m8 | حفظ کدهای قابل گسترش با محدودیت مستند، طبق انتخاب کاربر |
| m9 | قالب ثابت، endpointهای بازشده، statementهای مجزا و usingهای ساده اعمال شده |
| m10 | trace واقعی collector و metric hit/miss مشاهده شده؛ fallback/race تست‌ها پاس‌اند |
| m11 | مسیر host بدون User Secrets و بدون نیاز به تنظیمات قالب فراهم/آزموده شده |
| m12 | status تهی/نامعتبر Validation برمی‌گرداند و تست‌ها پاس‌اند |
