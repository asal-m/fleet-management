# قدم سوم؛ اصلاح دسته‌بندی خطاها (M2)

تاریخ: ۲۰۲۶-۱۰-۰۸. این تغییرات محلی هستند؛ هنوز commit یا push نشده‌اند.

## چه تغییر کرد و چرا؟

در MissionWorkflow و VehicleWorkflow همهٔ BusinessRuleValidationExceptionها
قبلاً به Conflict تبدیل می‌شدند. این کار تفاوت میان نبودن منبع، تعارض رزرو و
نقض قواعد کسب‌وکار را از بین می‌برد. اکنون factory خطای هر ماژول نوع قاعده را
دسته‌بندی می‌کند. شناسهٔ خطا، message key، آرگومان‌های پیام و RetryDirective.Never
حفظ شده‌اند. ثبت Audit تلاش ردشده همچنان در catch انجام می‌شود؛ failure از همان
مسیر قبلی برمی‌گردد تا تراکنش ناموفق وضعیت کسب‌وکار را commit نکند.

این تغییر سیاست تخصیص، تعمیر یا چرخهٔ مأموریت را عوض نمی‌کند؛ فقط دسته‌بندی
پاسخ ناموفق را با معنی خطا هماهنگ می‌کند.

## قرارداد خطاها

| نمونهٔ خطا | دسته | HTTP | gRPC در MP Core 0.9.3 |
|---|---|---:|---|
| ورودی نامعتبر در validator یا parser | Validation | 400 | InvalidArgument |
| MISSION_NOT_FOUND، VEHICLE_NOT_FOUND، DRIVER_NOT_FOUND | NotFound | 404 | NotFound |
| VEHICLE_ALREADY_RESERVED، DRIVER_ALREADY_RESERVED | Conflict | 409 | Aborted |
| VEHICLE_HAS_ACTIVE_MISSION و پلاک تکراری | Conflict | 409 | Aborted |
| VEHICLE_INACTIVE، VEHICLE_UNDER_MAINTENANCE | BusinessRule | 422 | FailedPrecondition |
| INSUFFICIENT_VEHICLE_CAPACITY، DRIVER_INACTIVE، DRIVER_UNQUALIFIED | BusinessRule | 422 | FailedPrecondition |
| MISSION_STATE_INVALID، MISSION_REASSIGNMENT_FORBIDDEN | BusinessRule | 422 | FailedPrecondition |
| SCHEDULED_TIME_NOT_FUTURE | BusinessRule | 422 | FailedPrecondition |
| MAINTENANCE_ALREADY_STARTED، MAINTENANCE_NOT_STARTED | BusinessRule | 422 | FailedPrecondition |
| هویت غایب یا نامعتبر / نقش نامجاز | امنیت | 401 / 403 | Unauthenticated / PermissionDenied |

قواعد دامنهٔ دیگر که در workflow شکست بخورند به‌طور پیش‌فرض BusinessRule هستند؛
اعتبارسنجی ورودی transport همچنان Validation است. منبع ناموجود هنگام Assign
همچنان domain برابر `operations` و message key قبلی را حفظ می‌کند.

در رقابت تخصیص و تعمیر، برنده‌شدن تخصیص باعث رد تعمیر با تعارض رزرو 409 می‌شود؛
برنده‌شدن تعمیر باعث رد تخصیص با قاعدهٔ VEHICLE_UNDER_MAINTENANCE و 422 می‌شود.
در هر دو حالت تست باید ثابت کند هر دو عملیات هم‌زمان موفق نشده‌اند.

## شواهد و روش آزمون

قبل از تغییر mapper، قرارداد جدید تخصیص روی کد قبلی اجرا شد: از ۹ حالت، ۷ مورد
شکست خوردند و دو تعارض رزرو پاس شدند. موارد شکست شامل پنج قاعدهٔ eligibility و
دو منبع ناموجود بودند؛ دستهٔ واقعی همهٔ آن‌ها Conflict بود. بنابراین تست‌ها
وجود ایراد قبلی را نشان دادند.

تست‌های Application اکنون علاوه بر هویت و دستهٔ خطا، ثابت‌ماندن Mission و ثبت
یک Audit ردشده بدون Audit موفق را بررسی می‌کنند. تست‌های REST با PostgreSQL و
Redis واقعی، Problem Details، state پس از شکست و Audit باقی‌مانده را بررسی
می‌کنند؛ پوشش منبع ناموجود، قواعد eligibility، lifecycle، تعمیر و زمان گذشته
اضافه یا اصلاح شده است. آزمون هشت تخصیص رقیب همچنان یک برنده و هفت تعارض 409
را انتظار دارد.

پنج حالت در FailureTransportTests از adapter واقعی نصب‌شدهٔ MP Core عبور
می‌کنند و status بومی gRPC و `grpc-status-details-bin` و ErrorInfo.domain/reason
را بررسی می‌کنند. آزمون واقعی نشان داد Conflict به Aborted نگاشت می‌شود؛
BusinessRule به FailedPrecondition. سرویس probe فقط در پروژهٔ تست ساخته و map
می‌شود. API کسب‌وکاری gRPC محصول همچنان فقط Query دارد؛ این آزمون شاهد اجرای
Command جدید از طریق gRPC محصول نیست. آزمون‌های gRPC محصول برای Query، 400/404
و احراز هویت و نقش‌ها در مجموعهٔ کامل باقی هستند.

فرمان اجرای مجموعهٔ کامل با وابستگی‌های تازه و رد هر skip:

```sh
dotnet run --project tools/VerifyTests/VerifyTests.csproj --configuration Release
```

## نتیجهٔ اجرای نهایی

اجرای کامل روی Windows با شناسهٔ `0a08c19f903146b08f3fceb8bae250b6`:

| پروژه | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Domain | ۹۱ | ۰ | ۰ |
| Application | ۳۳ | ۰ | ۰ |
| Integration | ۲۰ | ۰ | ۰ |
| مجموع | ۱۴۴ | ۰ | ۰ |

این ۱۴۴ مورد شامل ۹ حالت اضافه نسبت به مجموعهٔ قبلی ۱۳۵تایی هستند. فرمان با
exit code صفر تمام شد؛ migration، readiness و پاک‌سازی کانتینرها و volumeهای
موقت موفق بودند. فایل‌های TRX نیز مستقیم بررسی شدند: تعداد نتیجه‌ها با total
هر پروژه برابر بود و هر ۱۴۴ outcome مقدار Passed داشت. بنابراین هیچ تستی skip
نشده است. تست‌های معماری در همین مجموعهٔ Application پاس شدند.

اجرای اولیهٔ `7707b144e1534197953d3b15bfebcef2` با EOF در BuildKit قطع شد؛ لاگ
Docker Desktop علت توقف engine را پرشدن دیسک اعلام کرد. آن اجرا ناموفق است و
به‌عنوان شاهد پاس‌شدن تست‌ها شمرده نمی‌شود. پاک‌سازی خودکار آن به Docker دسترسی
نداشت؛ پس از بازیابی engine بررسی شد که هیچ کانتینر یا volume برای آن شناسه
وجود ندارد. خروجی موقت build ابزار probe که در همین قدم ساخته شده بود پاک شد؛
engine متوقف‌شده دوباره راه افتاد و اجرای کامل بعدی موفق شد.

محیط توسعه با همان volumeهای موجود دوباره بالا آمد؛ سپس با source اصلاح‌شده
build و اجرا شد و `/health/ready` پاسخ Healthy داد. حجم‌ها reset یا حذف نشدند.
نگاشت command failure در gRPC با probe تستی تأیید شده، نه endpoint تازهٔ محصول.
Linux CI و macOS در این قدم اجرا نشده‌اند.

## ابزارهای دستی و حدود این قدم

درخواست Postman مربوط به شروع دوبارهٔ مأموریت Completed از انتظار 409 به 422
اصلاح شد. انتظار پلاک تکراری و تعمیر خودروی دارای رزرو همچنان 409 است؛ assertion
SmokeClient برای تعمیر منبع رزروشده نیاز به تغییر نداشت. فایل‌های Postman از قبل
untracked بودند و به index تغییرات این قدم اضافه نشدند. مجموعهٔ Postman/Newman
در این قدم اجرا نشده است؛ پوشش خودکار REST در تست‌های Integration انجام می‌شود.

گزارش درخواست‌های قدم‌های قبلی شاهد همان نسخهٔ قبلی است و برای نمایش رفتار جدید
بازنویسی نمی‌شود. mapping خطای constraint یکتایی دیتابیس (m6)، nullable status
(m12)، بهبود قفل/کش (M4) و تأیید Linux CI (M1) موضوع قدم‌های دیگر هستند.
