# اجرای پروژه و کالکشن Postman

## اجرای پروژه

Docker Desktop باید روشن و روی Linux containers باشد. در Terminal از پوشهٔ
`FleetManagement` اجرا کن:

```powershell
docker compose up --build -d
docker compose ps
```

منتظر healthy شدن PostgreSQL و Redis بمان؛ آماده‌شدن API را از این آدرس ببین:
http://localhost:8080/health/ready — انتظار `Healthy`.

Swagger: http://localhost:8080/openapi-ui

برای دیدن خطای اجرا:

```powershell
docker compose logs --tail 100 application
```

برای توقف و حفظ اطلاعات: `docker compose down`.

## فایل‌های Import

| فایل | کاربرد |
|---|---|
| FleetOperations.postman_collection.json | کالکشن ۴۲ درخواست، شامل همهٔ ۱۸ عملیات OpenAPI |
| FleetOperations.Docker.postman_environment.json | محیط Docker؛ API روی 8080 و identity توسعه روی 55480 |
| FleetOperations.Rider.postman_environment.json | محیط Debug محلی؛ API روی 8180 و identity توسعه روی 18180 |

فایل‌ها token واقعی، password یا connection string ندارند. توکن‌های نقش‌ها هنگام
اجرا دریافت و در environment فعال ذخیره می‌شوند. base_url و identity_url متغیرند.

## Import و اولین Run

۱. فایل ZIP را Extract کن.
۲. در Postman Desktop، Import را بزن و فایل collection و فایل environment Docker را انتخاب کن.
۳. در environment selector محیط `Fleet Operations - Docker Local` را انتخاب کن.
۴. کالکشن را باز کن و Run collection را بزن.
۵. تمام پوشه‌ها را به ترتیب 00 تا 06 انتخاب کن؛ Iterations برابر 1 باشد.
۶. Run را بزن؛ بررسی‌ها باید پاس شوند. Delay لازم نیست.

اگر از Postman تحت وب استفاده می‌کنی، برای دسترسی به localhost از Desktop Agent
استفاده کن. راهنمای رسمی [Agent](https://learning.postman.com/docs/getting-started/basics/about-postman-agent/).

در برخی نسخه‌ها Run از منوی سه‌نقطهٔ کنار کالکشن قابل دسترسی است.
راهنمای رسمی [Collection Runner](https://learning.postman.com/docs/collections/running-collections/intro-to-collection-runs/).

ترتیب اجرا:

| پوشه | رفتار |
|---|---|
| 00 | دریافت token جدا برای FleetManager، Operator و Administrator از fixture Development |
| 01 | Health، سند OpenAPI و Platform status |
| 02 | ثبت منابع، بررسی Location راننده، GET جزئیات راننده و 404 برای شناسهٔ ناموجود؛ availability |
| 03 | Create → Schedule → Assign → Start → Complete؛ تعمیر خودروی رزروشده رد می‌شود و Audit آن خوانده می‌شود |
| 04 | شروع/پایان تعمیر، Inactive/Active کردن خودرو و بررسی availability |
| 05 | ایجاد مأموریت دوم، تخصیص منابع و Cancel؛ آزادشدن منابع |
| 06 | پاسخ‌های مورد انتظار 401، 403، 409 و 422 |

درخواست‌های منفی با 401/403/404/409/422 عمداً موفقیت HTTP ندارند؛ **پاس‌شدن Test** معیار
صحت آن‌هاست. این پاسخ‌ها در سناریوی منفی خطای کالکشن نیستند.

نام درخواست‌ها انگلیسی است تا method/operation هنگام ارائه مشخص باشد.
در Body، مقادیر ثابت نمونه هستند؛ baseStatus/status راننده Active=1 و Inactive=2.
پاسخ وضعیت‌ها نام‌دار است: Active/Inactive برای راننده و Draft، Scheduled، Assigned، InProgress، Completed، Cancelled برای مأموریت.
scheduled_time در Pre-request خودکار یک ساعت بعد از زمان فعلی UTC ساخته می‌شود.
پلاک خودکار یکتا است تا اجرای دوبارهٔ Runner duplicate نسازد.

## اجرای دستی با Send

اول پوشهٔ 02 را به ترتیب اجرا کن، سپس Create و Schedule در پوشهٔ 03 را اجرا کن؛
بعد Assign را Send کن. vehicle_id، driver_id و mission_id از پاسخ‌ها ذخیره می‌شوند.
نیازی به کپی دستی ID یا Bearer token نیست. هر درخواست نقش صحیح خودش را دارد.
tokenهای پنج‌دقیقه‌ای در Pre-request در صورت نبودن یا نزدیک انقضا تمدید می‌شوند.
روش اسکریپت مطابق [pm.sendRequest](https://learning.postman.com/docs/tests-and-scripts/write-scripts/postman-sandbox-reference/pm-send-request/) است.

اگر فقط Complete را بدون اجرای Start بفرستی، state transition رد می‌شود؛ این
رفتار دامنه است. از روی یک مأموریت Completed دوباره Assign نکن؛ Runner را از
ابتدا اجرا کن تا شناسه‌های جدید ساخته شوند. اجرای Runner داده و Audit مصنوعی
واقعی می‌سازد و آن‌ها را حذف نمی‌کند.

Queryهای فهرست limit=200 دارند؛ assertion حضور entity نمونه برای دیتابیس کوچک
توسعه مناسب است. اگر بیش از ۲۰۰ منبع یا مأموریت واجد شرایط وجود داشته باشد،
نمونه ممکن است بیرون صفحه باشد؛ این سناریو جایگزین تست pagination یا load نیست.

## Debug در Rider

برای این محیط، راهنمای [راه‌اندازی محلی](../../docs/local-development.md)
را اجرا کن. API را در Rider با Debug و identity محلی را روی 18180 روشن کن؛ سپس
environment را به `Fleet Operations - Rider Local` تغییر بده. identity Docker
برای API محلی این configuration مناسب نیست چون issuer و signing key متفاوت است.

در زمان breakpoint طولانی، timeout Postman را در Settings افزایش بده و از
Send دستی استفاده کن. برای demo Runner همهٔ breakpointها را mute کن.

## عیب‌یابی

| مشکل | بررسی |
|---|---|
| Connection refused | Docker/application روشن باشد؛ base_url محیط درست باشد |
| متغیر حل‌نشده مثل {{mission_id}} | environment را انتخاب کن و درخواست‌های قبل را به ترتیب اجرا کن |
| Token request شکست خورد | identity_url و fixture همان محیط روشن باشند |
| 401 در درخواست عادی | محیط درست؛ دریافت توکن با issuer همان API؛ ساعت سیستم درست |
| 403 در درخواست عادی | Authorization درخواست را تغییر نداده باشی؛ نقش موردنیاز در Description آمده |
| 422 در lifecycle | ترتیب stateها و شناسهٔ مأموریت فعلی را بررسی کن |
| Runner منتظر می‌ماند | API روی breakpoint متوقف نباشد |

این کالکشن HTTP است؛ Swagger/OpenAPI endpointهای REST را توصیف می‌کند.
gRPC از protoهای `src/FleetCompany.FleetManagement.Api/Protos` استفاده می‌کند و
در این کالکشن HTTP قرار نگرفته است. Token fixture و Health درخواست‌های کمکی‌اند؛
endpoint جدیدی به محصول اضافه نشده است.

## بررسی به‌روزرسانی GetDriver

درخواست GET /api/drivers/{{driver_id}} و حالت 404 به هر دو نسخهٔ کالکشن اضافه
شده‌اند. پاسخ ثبت راننده نیز از نظر Location بررسی می‌شود. کالکشن‌ها یکسان‌اند
و JSON و syntax همهٔ scriptها بررسی شده‌اند؛ این به‌روزرسانی در Postman/Newman
اجرا نشده است. تست Integration موجود، GET از Location و DRIVER_NOT_FOUND را
در اجرای قبلی مجموعهٔ ۱۷۹تستی تأیید کرده است.

## شاهد بررسی نسخهٔ قبلی

کالکشن با OpenAPI زنده تطبیق داده شد: پوشش ۱۸ عملیات از ۱۸ عملیات.
درخواست‌ها و Pre-request/Post-response scriptها با یک adapter Node روی API Docker
واقعی اجرا شدند: **۴۲ درخواست، ۷۱ assertion و تمدید token منقضی‌شده موفق**.
Postman/Newman در این محیط اجرا نشده؛ نتیجهٔ فوق مربوط به اجرای adapter است.
در اولین بررسی مقدار عددی AuditOutcome.Rejected اصلاح شد: قرارداد واقعی مقدار 1
و failure.code برابر VEHICLE_HAS_ACTIVE_MISSION را برمی‌گرداند. اجرای نهایی پاس شد.
رکوردهای مصنوعی آزمون و Auditهایشان برای مشاهده باقی مانده‌اند.
