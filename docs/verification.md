# گزارش پوشش چالش و راستی‌آزمایی

آخرین بازبینی مستندات: ۲۰۲۶-۱۰-۰۹؛ MP Core 0.9.3 و .NET 10.

## جدیدترین شاهد؛ اصلاحات فعلی

suite فعلی روی Windows و داخل Linux هرکدام **۱۷۹ Passed، صفر Failed و Skipped**
داشتند؛ image فعلی محصول build شد و Smoke واقعی REST/gRPC، metric hit/miss و
trace در Jaeger تأیید شدند. [گزارش اصلاحات فعلی](verification-current-review-fa.md)
تلاش‌های ناموفق harness، محدودیت اجرای CI و موارد باز کسب‌وکار را جدا ثبت می‌کند.

## شاهد قبلی؛ قدم هفتم

API میزبان با وابستگی‌های مستقل Docker از volume خالی راه افتاد و پس از down/up
دوباره بررسی شد. هر دو اجرا **۱۷۲ Passed، صفر Failed و Skipped** داشتند؛ TRX سخت‌گیر
پاس شد. Smoke واقعی REST/gRPC و JWT/Audit، اجرای هم‌زمان دو محیط و حفظ credentials
و تمام منابع قبلی تأیید شدند. [گزارش قدم هفتم](verification-step-7-fa.md) شکست‌های
میانی و اصلاحشان را هم ثبت می‌کند. شاهد Windows است؛ native Linux/macOS و build
سرد هنوز تأیید نشده‌اند. گزارش‌های زیر تاریخچهٔ مراحل قبلی‌اند.

## شاهد اجرایی اخیر و وضعیت ارسال مجدد

در اجرای ثبت‌شدهٔ ۲۰۲۶/۱۰/۰۸، روی کد با commit پایهٔ
3a61cf094e1efb49566e8ee3238a948f412b29da، هر ۱۳۵ تست شامل ۹۱ Domain، ۳۱
Application و ۱۳ Integration بدون شکست و skip پاس شدند. Docker با source فعلی
build شد؛ ۷۰ بررسی REST، چهار Query کسب‌وکاری gRPC، Probe/Health/Reflection و
قطع/وصل Redis پاسخ مورد انتظار داشتند. جزئیات و محدودیت‌ها در
[گزارش اجرای اخیر](runtime-review-2026-10-08-fa.md) و
[نتایج درخواست‌ها](endpoint-results-2026-10-08.json) ثبت شده‌اند.

مرحلهٔ اول اصلاح ارزیابی فقط ignoreها، مستندات و بستهٔ تحویل را تغییر داده است؛
در این مرحله تست محصول دوباره اجرا نشده است. اجرای قبلی روی Windows و volumeهای
موجود بود؛ گزارش‌های تاریخی نصب تمیز زیر، متعلق به اجراهای قبلی‌اند.

در قدم دوم، ابزار C# برای اجرای کامل تست‌ها با وابستگی‌های تازه و بدون skip اضافه
شد و روی Windows هر ۱۳۵ تست را پاس کرد. جزئیات و کنترل‌های منفی در
[شاهد قدم دوم](verification-step-2-fa.md) هستند. اجرای داخل کانتینر Linux روی میزبان
Windows به دلیل دسترسی به پورت loopback ناموفق شد؛ شاهد Linux CI هنوز موجود نیست.

موفقیت تست‌ها پذیرش کامل چالش نیست. پذیرش چندسکویی M1 منتظر اجرای Linux CI است؛
در قدم سوم، دسته‌بندی failureهای M2 اصلاح شد و هر ۱۴۴ تست با صفر شکست و skip
پاس شدند؛ قرارداد و شواهد آن در [گزارش قدم سوم](verification-step-3-fa.md) ثبت
شده‌اند. سایر موارد گزارش طبق
[وضعیت اصلاحات](remediation-status.md) باز هستند. D16 نیز به‌عنوان موضوع تفسیر
storage داخلی چارچوب باقی می‌ماند.

## شاهد قدم چهارم؛ A1/A2 و M3

جدیدترین اجرای کامل در قدم ششم: **۱۷۲ Passed؛ صفر Failed و Skipped**، شامل ۹۱
Domain، ۴۴ Application و ۳۷ Integration. مالکیت ماژول‌ها، validation زمان در
Application، guard وضعیت ثبت و نگاشت REST/gRPC بررسی شدند. جزئیات و محدودیت
imageهای راه‌اندازی قدیمی و CI در [گزارش قدم ششم](verification-step-6-fa.md) هستند.
اجرای زیر شاهد تاریخی قدم چهارم است.

اعتبارسنج TRX به تعداد و outcome تک‌تک تست‌ها متصل شد؛ ۱۲ آزمون regression و
کنترل گزارش ناقص پاس شدند. ابزار اکنون تصاویر تولیدشدهٔ تست و builder و کش
اختصاصی هر اجرا را پاک می‌کند و منابع مشترک را حفظ می‌کند. در محیط با volumeهای
تازه و imageهای محلیِ قدم سوم، تست‌ها از source فعلی build شدند و ۹۱ Domain،
۳۳ Application و ۳۳ Integration، مجموع **۱۵۷ Passed و صفر شکست/skip** داشتند.
پاک‌سازی مسیر شکست و محیط موفق کنترل‌شده بررسی شد؛ منابع قبلی حفظ شدند.
build سرد با builder اختصاصی هنوز به دلیل دانلود بسیار کند موفق مشاهده نشده است.
سیاست رزرو/زمان M3 مستند و آزموده شد؛ جزئیات و محدودیت‌ها در
[گزارش قدم چهارم](verification-step-4-fa.md) و
[D17](decisions/D17-reservation-and-schedule-policy.md) هستند. Linux CI همچنان باز است.

## شاهد قدم پنجم؛ بهینه‌سازی محدود M4

نسخهٔ کش فقط با تغییر عضویت خودروهای available افزایش می‌یابد؛ ثبت راننده
قفل خودرو را نمی‌گیرد. Audit تغییرهای واقعی و قفل منابع رزروشده حفظ شدند.
دو regression روی کد قبلی شکست خوردند؛ اجرای کامل کد اصلاح‌شده **۱۶۰ Passed،
صفر شکست و صفر skip** داشت. ۲۶ کنترل revision، هشت ثبت راننده زیر قفل جدا،
رقابت‌های موجود و کش دو میزبان پاس شدند. ده خواندن پس از ثبت راننده به‌جای
۴۰ فرمان EF، بیست فرمان اجرا کردند؛ latency گرم در اندازه‌گیری بعد بیشتر شد،
پس سرعت کلی بهتر ادعا نمی‌شود. [گزارش قدم پنجم](verification-step-5-fa.md)
و [دادهٔ اندازه‌گیری](measurements/m4-before-after.json) حدود شاهد را ثبت می‌کنند.
volumeهای وابستگی تازه بودند و میزبان‌های HTTP/gRPC از source جدید build شدند؛
image محصول Docker جدید ساخته نشده است. قفل ریزتر، build سرد و Linux CI باقی‌اند.

## یادداشت‌های تاریخی توسعه؛ از ۲۰۲۶-۱۰-۰۵

مطالب زیر برای حفظ شواهد قبلی نگه داشته شده‌اند؛ عبارت‌های «آخرین» یا «اصلاح‌شده»
در آن‌ها مربوط به زمان همان یادداشت‌اند و جای وضعیت ارسال مجدد بالا را نمی‌گیرند.

## نتیجه

بازبینی جدید: راه‌اندازی مستقیم Compose برای نصب تازه اصلاح و با volume خالی
تأیید شد؛ محیط موجود نیز با واردکردن رمزهای قبلی ارتقا یافت. یادداشت AI بر اساس
تأیید توسعه‌دهنده دربارهٔ استفاده از Codex بازنویسی و تاریخچه حفظ شد. شاهد API
نسخهٔ ۰٫۹٫۳ و source رسمی در [D16](decisions/D16-framework-message-storage.md)
ثبت شد؛ چارچوب گزینهٔ مستند خاموش‌کردن این storage را ارائه نمی‌کند و D16 باز است.

قابلیت‌های اصلی پیاده و آزموده شده‌اند. **انطباق کامل بدون قید هنوز ادعا نمی‌شود:
D16 باز است** و دربارهٔ ذخیره‌سازی داخلی Wolverine در برابر ممنوعیت Outbox/Inbox
چالش، نیازمند تأیید ارزیاب یا راه رسمی تنظیم چارچوب است.
هیچ کد چارچوب تغییر نکرده و broker یا workflow Outbox/Inbox محصول اضافه نشده است.

## شاهد اجرا

- اجرای کامل از scripts/Start-LocalDependencies.ps1 -RunTests:
  ۹۱ Domain، ۲۸ Application و ۱۳ Integration؛ مجموع **۱۳۲ موفق، صفر شکست و صفر skip**.
- Integration از JWT/RSA واقعی، Handler و تراکنش واقعی میزبان، PostgreSQL و Redis استفاده می‌کند؛
  فقط OIDC metadata تست کنترل‌شده است، نه اعتبارسنجی token.
- docker compose up --build -d با برنامه، PostgreSQL، Redis و ابزار کوچک هویت Development اجرا شد.
- SmokeClient روی socketهای واقعی: REST lifecycle، چهار Query gRPC، OIDC discovery/JWKS واقعی،
  منع دسترسی ناشناس، رد تعمیر خودرو رزروشده و Audit موفق/ردشده پاس شد.
- Verify-RedisFailure.ps1: توقف Redis همین پروژه، خواندن availability با کلید تازه از DB،
  Healthy ماندن readiness و صحت وضعیت جدید پس از بازگشت Redis پاس شد.
- تست correlation، TraceId واحد را در Transport/Application/Domain/Infrastructure و Npgsql
  و span عملیات Hybrid Cache بررسی کرد.
- migrationها اعمال شدند؛ framework EF tool نسخهٔ 10.0.10 در برابر runtime 10.0.11
  هشدار نسخهٔ جزئی می‌دهد، اما migration و تست‌ها موفق‌اند.
- حساب مالک migration جداست؛ حساب runtime برای Audit فقط SELECT/INSERT و sequence usage دارد.
  SmokeClient در ترمینال Initialize-ComposeEnvironment، این مجوزها را به‌صورت فقط‌خواندنی بررسی می‌کند.

هر عدد، نتیجهٔ یک اجرای واقعی است؛ تعداد Factها با تعداد سناریوهای درون هر Fact برابر نیست.
Auditهای تست پاک نمی‌شوند. بدون متغیرهای fixture، Integrationها skip می‌شوند؛ آن اجرا
شاهد کامل نیست.

## نگاشت ۱۳ سناریوی الزامی بخش ۱۵

| # | سناریو | شاهد | وضعیت |
|---|---|---|---|
| ۱ | تخصیص خودروی Inactive | AssignmentEligibilityTests + OperationsAcceptanceTests | ☑ |
| ۲ | تخصیص خودروی زیر تعمیر | همان تست‌ها؛ رد همراه Audit | ☑ |
| ۳ | تخصیص رانندهٔ Inactive | همان تست‌ها | ☑ |
| ۴ | رانندهٔ فاقد صلاحیت | همان تست‌ها | ☑ |
| ۵ | ظرفیت ناکافی | همان تست‌ها | ☑ |
| ۶ | تداخل رزرو خودرو | OperationsAcceptanceTests؛ بدون تغییر مأموریت بازنده | ☑ |
| ۷ | تداخل رزرو راننده | OperationsAcceptanceTests | ☑ |
| ۸ | انتقال وضعیت نامعتبر | MissionTests؛ ماتریس وضعیت‌ها + HTTP acceptance | ☑ |
| ۹ | تلاش تخصیص هم‌زمان | هشت درخواست؛ یک موفق و هفت conflict؛ رقابت جدا با تعمیر | ☑ |
| ۱۰ | ابطال کش پس از تغییر availability | دو میزبان؛ Assign/Complete/Status/Maintenance | ☑ |
| ۱۱ | ایجاد Business Audit | تمام هشت عملیات الزامی؛ Actor و rejection و تکرار بدون duplicate | ☑ |
| ۱۲ | شکست Authorization | REST/gRPC، نقش نادرست؛ JWT بدون token/امضا/مخاطب معتبر | ☑ |
| ۱۳ | رفتار یکسان REST و gRPC | VehicleGrpcTests + OperationsGrpcTests | ☑ |

## پوشش بندهای چالش

| بند | پوشش و محدودیت |
|---|---|
| ۱ | manifest و CLI تولیدشده با همهٔ انتخاب‌های الزامی؛ قابلیت‌ها واقعی استفاده می‌شوند |
| ۲–۳ | چهار bounded context و Contracts مستقل؛ مرز مالکیت داده |
| ۴–۵ | ثبت خودرو/راننده، نوع قابل گسترش، ظرفیت، وضعیت، تعمیر و صلاحیت |
| ۶–۷ | چرخهٔ کامل و تمام قواعد Assign؛ هماهنگی تراکنشی و قید دیتابیس |
| ۸ | شروع/پایان تعمیر؛ رد خودرو رزروشده، بدون لغو خودکار |
| ۹ | Hybrid Cache، کلید، TTL، نسخهٔ مشترک، stale snapshot و شکست Redis |
| ۱۰ | Audit موفق و ردشده، Actor واقعی و خواندن Administrator |
| ۱۱ | تمام عملیات REST جدول چالش؛ validation و failure مشترک |
| ۱۲ | هر چهار Query gRPC با Application مشترک |
| ۱۳ | احراز هویت bearer/OIDC و نقش‌ها؛ هویت آزمایشی فقط Development |
| ۱۴ | Codex + skills قالب؛ تولید، اصلاح و توصیه‌های ردشده در یادداشت AI |
| ۱۵ | سه سطح تست و همهٔ ۱۳ سناریو |
| ۱۶ | log، correlation، tracing، failure، DB و cache؛ تست زنجیرهٔ درخواست |
| ۱۷ | Dockerfile و Compose کامل؛ PostgreSQL/Redis نصب دستی لازم ندارند |
| ۱۸ | Kafka/RabbitMQ/TimeSeries/Microservices استفاده نشده؛ **D16 باز** |
| ۱۹ | source، README، architecture، AI notes، migration، REST/proto و tests موجودند |
| ۲۰ | معیارهای اجرایی در تست/Compose بررسی شده‌اند؛ اعلام پذیرش نهایی وابسته به D16 است |
| ۲۱–۲۲ | trade-offها و راهنمای توضیح در مصاحبه؛ نمره/پذیرش را فقط ارزیاب تعیین می‌کند |

## ۱۵ کار انجام‌شده در ادامهٔ مجازشده

| کار | خروجی |
|---|---|
| ۱ | تفکیک Contracts و جهت وابستگی‌ها |
| ۲ | قراردادهای فقط‌خواندنی Fleet/Drivers/Operations |
| ۳ | هماهنگ‌کنندهٔ تراکنشی availability در PostgreSQL |
| ۴ | قواعد کامل تخصیص داخل Domain |
| ۵ | Schedule/Assign/Start/Complete/Cancel در Application |
| ۶ | تغییر وضعیت و Start/Complete Maintenance |
| ۷ | Queryهای خودرو/رانندهٔ در دسترس و مأموریت‌های فعال |
| ۸ | Hybrid Cache، نسخهٔ مشترک، TTL و fallback |
| ۹ | Audit همهٔ عملیات الزامی و rejection |
| ۱۰ | endpointهای REST و validation |
| ۱۱ | چهار قرارداد و سرویس gRPC |
| ۱۲ | سیاست نقش‌ها، OIDC محلی مستقل و محدودیت DB Audit |
| ۱۳ | migration و محیط کامل Docker |
| ۱۴ | تست‌های هم‌زمانی/کش/امنیت/برابری transports و observability |
| ۱۵ | README، معماری تصمیم‌محور، AI notes، گزارش و راهنمای مصاحبه |

## آنچه هنوز باز است

**D16 — باز؛ نیازمند تأیید.** مشاهدهٔ دیتابیس نشان داده است جدول‌های
wolverine_incoming_envelopes و wolverine_outgoing_envelopes از foundation تولید
می‌شوند. messaging none تضمین حذف آن‌ها نیست. حذف کورکورانهٔ جدول/بسته یا patch
چارچوب انجام نشده است. اگر ارزیاب storage داخلی framework را هم ممنوع بداند،
نیازمند مسیر رسمی سازگار یا توافق صریح بر نسخه/پیکربندی هستیم.

استقرار Production، provider واقعی هویت، TLS، retention عملیاتی Audit، backup و
load test در این تحویل محلی اجرا نشده‌اند؛ این‌ها شاهد محیط عملیاتی محسوب نمی‌شوند.
قفل سراسری throughput را محدود می‌کند و Query availability یک snapshot است.
ثبت موجودیت‌ها idempotency key مستقل ندارد؛ تکرار چرخهٔ Mission طبق تصمیم دامنه
کنترل شده است. پروژهٔ Source محلی تحویل است؛ انتشار GitHub/ساخت تاریخچهٔ commit
انجام نشده و برای ارزیابی commit quality باید commitهای واقعی خود توسعه‌دهنده موجود باشند.


## شاهد نهایی تکمیلی

نسخهٔ نهایی دوباره ۱۳۲ تست را با صفر شکست و صفر skip پاس کرد.
SmokeClient مجوزهای runtime Audit را از PostgreSQL بررسی کرد: SELECT/INSERT
مجاز و UPDATE/DELETE/TRUNCATE نامجاز؛ REST روی listener gRPC پاسخ 404 داد.
همچنین در پروژهٔ Docker آزمایشی جدا با volume خالی، migrationها خودکار اعمال
شدند و readiness، REST، هر چهار gRPC، OIDC و Audit موفق/ردشده پاس شدند.
این بررسی از دیتابیس قبلی آماده استفاده نکرد؛ محیط آزمایشی جدا پس از بررسی
جمع می‌شود و محیط اصلی برای ادامهٔ کار حفظ می‌شود. D16 تغییری نکرده است.

## اجرای اصلاحات بازبینی — ۲۰۲۶-۱۰-۰۵

- docker compose config --quiet بدون متغیر اتصال یا User Secrets موفق شد.
- ابزار ComposeBootstrap با صفر warning/error ساخته شد؛ سه image محیط ساخته شدند.
- در پروژهٔ جدا با volumeهای خالی و پورت‌های آزمایشی، up --build موفق شد؛ bootstrap
  رمزها را تولید کرد، PostgreSQL/Redis healthy شدند و migrationهای برنامه اجرا شدند.
- Test-ComposeEnvironment.ps1 روی همان محیط تازه: **۹۱ Domain، ۲۸ Application،
  ۱۳ Integration؛ مجموع ۱۳۲ موفق، صفر شکست و صفر skip**.
- Verify-ComposeSmoke.ps1 روی محیط تازه: readiness، جداسازی listener، مجوزهای
  SELECT/INSERT-only برای Audit، REST lifecycle، هر چهار gRPC، OIDC discovery،
  رد دسترسی ناشناس، رد تعمیر خودروی رزروشده و Audit موفق/ردشده پاس شد.
- Verify-ComposeBootstrap.sh در کانتینر موقت: فایل‌های 0600، حفظ رمز پس از retry،
  بازسازی فایل مشتق حذف‌شده، عدم تغییر رمز با environment جدید، رد state خراب
  بدون overwrite و رد import ناقص تأیید شد. هیچ volume نصب موجود دستکاری نشد.
- در کانتینر موقت، Production استفاده از فایل تنظیمات Development را رد کرد.
- نصب موجود با Initialize-ComposeEnvironment فقط یک بار وارد نسخهٔ جدید شد؛
  volume قبلی PostgreSQL حفظ شد و رکوردهای قبلی خودرو و مأموریت باقی بودند.
  اجرای بعدی Compose از ترمینال بدون initialization نیز موفق شد.
- Compose محیط تازه دوباره اجرا شد؛ تنظیمات اتصال قبل و بعد به‌صورت مقایسه در
  حافظه یکسان بود؛ هیچ رمز یا hash آن چاپ نشد.
- Socket smoke محیط اصلیِ ارتقایافته نیز همان بررسی‌های REST/gRPC/Audit را پاس کرد.
- Verify-RedisFailure.ps1 روی نسخهٔ جدید: توقف Redis، خواندن کلید تازه از PostgreSQL،
  Healthy ماندن readiness و وضعیت درست availability پس از بازگشت Redis پاس شد.

### خطاهای دیده‌شده و اصلاح واقعی

اولین build ابزار bootstrap خطای CA1416 برای setter مربوط به UnixCreateMode
در Windows داشت؛ setter به شاخهٔ صریح non-Windows منتقل شد. analyzer یا الزام
TreatWarningsAsErrors تغییر نکرد. build بعدی و build Docker موفق شدند.

در ارتقای نصب قبلی، Redis با UID جدید تلاش کرد dump.rdb قدیمیِ cache را از volume
ناشناس قبلی بخواند و Permission denied داد. مسیر cache به /tmp تغییر کرد و
snapshot/AOF غیرفعال ماند؛ فایل یا volume قبلی حذف نشد. PostgreSQL قبلی سالم بود.
پس از اصلاح، Compose کامل و Redis healthy شدند. این شکست مربوط به محیط قدیمی
بود؛ اجرای اولیه با volume خالی و suite کامل قبلاً موفق شده بودند.

محدودیت نهایی: پذیرش منع Outbox/Inbox در برابر storage داخلی MP Core همچنان
نیازمند نظر ارزیاب یا API رسمی سازگار است. گزارش تغییر دستی انسانی مستقل ساخته
نشده؛ توسعه‌دهنده انجام کار با کمک Codex را تأیید کرد.

## رفع موارد ۳، ۴ و ۵ بازبینی — ۲۰۲۶-۱۰-۰۵

- سه ArchitectureTests به suite اضافه شد: ProjectReference میان ماژول‌ها فقط به
  Contracts؛ نبود استفاده از EF/ASP.NET/gRPC/broker در Domain/Application؛ وجود متن
  غیرخالی انگلیسی و فارسی برای کلیدهای BusinessRule، validator و FailureMessageDescriptor.
  تحلیل وابستگی و کلیدها با Roslyn است و alias و نام کاملاً qualified را نیز می‌بیند؛
  خطای تحلیل/کامپایل یا کلید غیرقابل‌بررسی باعث شکست تست می‌شود.
- هر سه محافظ با نقض موقت بررسی شدند: ارجاع Fleet به پروژهٔ اصلی Drivers، alias
  DbContext در Domain و متن خالی فارسی. هرکدام تست مربوط را با علت مورد انتظار
  شکست دادند؛ بایت‌های فایل اصلی در finally برگردانده شد. این شکست‌ها عمدی بودند.
- scripts/Verify-FreshCompose.ps1 پروژهٔ مستقل fleet-verification-d7e2ea0ed158 را
  پس از تأیید نبود کانتینر و volume قبلی ایجاد کرد. credentialهای نصب موجود وارد
  نشدند؛ docker compose up --build -d موفق بود، bootstrap خودکار و migrationها از
  دیتابیس خالی اجرا شدند و readiness سالم شد. Build از cache مجاز Docker استفاده کرد.
- suite روی همان محیط تازه: **۹۱ Domain، ۳۱ Application و ۱۳ Integration؛ مجموع
  ۱۳۵ موفق، صفر شکست و صفر skip**.
- Socket smoke روی محیط تازه: چرخهٔ REST، هر چهار متد gRPC، OIDC واقعی، رد دسترسی
  ناشناس، رد تعمیر خودروی رزروشده، Audit موفق/ردشده و مجوزهای runtime Audit پاس شدند.
- Verify-RedisFailure.ps1 اکنون نام پروژه و پورت‌ها را می‌پذیرد. Redis فقط در محیط
  آزمایشی متوقف شد؛ Query با revision تازه از PostgreSQL پاسخ گرفت و readiness
  Healthy ماند. Redis دوباره شروع شد و Query وضعیت جدید Inactive را درست منعکس کرد.
- finally محیط آزمایشی کانتینرها، network و volumeهای همان پروژه را حذف کرد؛ بررسی
  label پروژه نبود کانتینر/volume باقی‌مانده را تأیید کرد. محیط fleetmanagement-local
  حفظ شد و PostgreSQL و Redis اصلی healthy ماندند.
- مورد ۵ با اصلاح قرارداد مستند حل شد: Vehicle/Mission پاسخ 201 همراه Location
  دارند؛ Driver پاسخ 201 همراه representation و بدون Location دارد، چون Get Driver
  با شناسه در scope فعلی وجود ندارد. endpoint یا مسیر Location غیرقابل‌خواندن اضافه نشد.

بازتولید بررسی تازه: `.\scripts\Verify-FreshCompose.ps1`؛ پورت‌های پیش‌فرض مستقل
در README آمده‌اند و قابل تغییرند. موارد D16 و گزارش مشارکت/بازبینی دستی انسانی
با این تغییرها حل‌شده اعلام نمی‌شوند.
