# D18؛ نسخهٔ کش متناسب با تغییر availability و دامنهٔ هماهنگی

به‌روزرسانی: بخش قفل سراسری این سند مربوط به قدم پنجم است؛ طراحی فعلی قفل هر
منبع در [D21](D21-resource-locks-and-readable-rest-states.md) آمده است. سیاست
افزایش نسخه و دو خواندن DB این سند همچنان معتبرند.

تاریخ: ۲۰۲۶-۱۰-۰۹؛ بررسی اولیه ۲۰۲۶-۱۰-۰۸. محدوده: بهینه‌سازی محدود M4؛ سیاست رزرو D17 حفظ می‌شود.

نسخهٔ PostgreSQL برای کش **available vehicles** است، نه تمام تغییرهای سیستم.
عضویت خودرو در این مجموعه به Active بودن، خارج از تعمیر بودن و نداشتن رزرو
فعال وابسته است. GET available drivers در نسخهٔ فعلی کش نسخه‌دار ندارد و مستقیماً
از read model خوانده می‌شود. بنابراین ایجاد راننده، کش خودروها را باطل نمی‌کند.

## جدول اثر commandها

| عملیات موفق | اثر بر نسخهٔ کش خودرو | هماهنگی |
|---|---|---|
| RegisterVehicle؛ Active | افزایش ۱ | قفل موجود؛ بررسی پلاک و ثبت |
| RegisterVehicle؛ Inactive | بدون افزایش | همان قفل برای رقابت پلاک |
| RegisterDriver | بدون افزایش | ثبت aggregate تازه با ID تولیدشده در سرور؛ بدون قفل خودرو |
| CreateMission؛ Draft | بدون افزایش | مسیر ثبت موجود |
| Schedule، تکرار یا زمان تازه در Scheduled | بدون افزایش | قفل موجود برای mutation همان Mission |
| Assign جدید | افزایش ۱؛ منابع فوراً رزرو می‌شوند | قفل موجود برای بررسی eligibility و رزرو هر دو منبع |
| Assign تکراری مجاز | بدون افزایش | قفل موجود |
| Start، Start تکراری | بدون افزایش؛ رزرو همچنان فعال است | قفل موجود برای Mission |
| Complete از InProgress | افزایش ۱؛ رزرو آزاد می‌شود | قفل موجود |
| Cancel از Assigned | افزایش ۱؛ رزرو آزاد می‌شود | قفل موجود |
| Cancel از Draft/Scheduled | بدون افزایش | قفل موجود |
| Complete/Cancel تکراری | بدون افزایش | قفل موجود |
| تغییر وضعیت پایه یا شروع/پایان تعمیر | افزایش فقط هنگام تغییر IsOperationalForAssignment | قفل موجود برای سازگاری با Assign و قواعد منبع |
| خطای validation، منبع غایب یا قاعدهٔ ردشده | بدون افزایش | مسیر failure و rollback موجود |

مثال: شروع و پایان تعمیر روی خودروی Inactive، آن را وارد/خارج available نمی‌کند؛
Active کردن خودرو هنگام تعمیر نیز تا CompleteMaintenance اثری در این مجموعه ندارد.
قواعد محصول، تعمیر یا Inactive کردن منبع رزروشده را منع می‌کنند؛ بنابراین تغییر
واجدشرایط‌بودن Fleet در مسیر مجاز، همان تغییر عضویت availability است.

## جداسازی تغییر کسب‌وکار، Audit و ابطال کش

MissionWorkflow پیش و پس از عملیات HasActiveReservation را مقایسه می‌کند؛
VehicleWorkflow همین مقایسه را برای IsOperationalForAssignment انجام می‌دهد.
Audit همچنان برای تغییر کسب‌وکاری واقعی ثبت می‌شود، حتی اگر نسخهٔ کش تغییر نکند.
مثلاً Schedule و Start همچنان Audit دارند. تکرارهای مجاز و failureها مانند قبل
تاریخچهٔ موفق تازه نمی‌سازند؛ Audit تلاش ردشده و مسیر rollback محفوظ است.

افزایش نسخه زیر همان تراکنش باقی می‌ماند؛ middleware چارچوب مالک commit است.
مرز ماژول‌ها، قرارداد HTTP/gRPC، جدول‌ها و migrationها تغییر نمی‌کنند.

## علت محدودکردن تغییر قفل

ثبت راننده aggregate تازه‌ای را با ID سروری ایجاد می‌کند؛ قبل از commit، منابع
موجود یا رزرو مأموریت دیگری را تغییر نمی‌دهد. این مسیر به قفل هماهنگی خودروها
نیاز ندارد. تست واقعی با نگه‌داشتن همان advisory lock روی connection جدا و هشت
ثبت هم‌زمان، پیشرفت مستقل این مسیر را بررسی می‌کند.

قفل سراسری Assign، تغییر وضعیت/تعمیر خودرو و گذارهای Mission حفظ می‌شود.
این قدم ادعای هم‌زمانی تخصیص دو منبع مستقل یا حذف گلوگاه همهٔ writeها ندارد.
قفل ریزتر به کلید پایدار خودرو/راننده/مأموریت، ترتیب کلی ثابت، هماهنگی با
Complete/Cancel/تعمیر، آزمون deadlock و رقابت و بررسی isolation نیاز دارد.
unique index شرطی روی رزرو هر دو منبع همچنان آخرین سد است؛ version روی Mission
به‌تنهایی مالکیت یک منبع مشترک بین دو Mission را تضمین نمی‌کند.

## هزینهٔ کش و حدود شاهد

کلید نسخه‌دار، TTL برابر ۳۰ ثانیه، دو خواندن نسخه از DB و بازخوانی پس از تغییر
نسخه حفظ شده‌اند. cache hit همچنان بدون roundtrip دیتابیس نیست. هیچ revision
به Redis منتقل نشده و ابطال پس از commit یا tag invalidation فرض نشده است.
cache factory و fallback Redis تغییر نمی‌کنند. صحت تصمیم Assign به کش وابسته نیست.

اندازه‌گیری با شمارندهٔ مستقیم EF و درخواست واقعی TestServer انجام می‌شود؛
cache سرد، ۲۰ خواندن گرم، ۱۰ ثبت راننده همراه خواندن و نگه‌داشتن قفل به مدت
۳۵۰ میلی‌ثانیه اندازه‌گیری می‌شوند. این microbenchmark محلی، هدف ظرفیت عملیاتی
یا SLO را ثابت نمی‌کند؛ میانگین/p95 به محیط، JIT و شبکه وابسته‌اند.
نتیجهٔ واقعی و محدودیت اجرا در [گزارش قدم پنجم](../verification-step-5-fa.md)
ثبت شده است؛ عدد فرضی به‌عنوان نتیجه درج نمی‌شود.

## شواهد کد و آزمون

- [MissionWorkflow](../../src/Modules/Operations/FleetCompany.FleetManagement.Modules.Operations/Application/Commands/MissionWorkflow.cs)
- [VehicleWorkflow](../../src/Modules/Fleet/FleetCompany.FleetManagement.Modules.Fleet/Application/Commands/VehicleWorkflow.cs)
- [RegisterVehicle](../../src/Modules/Fleet/FleetCompany.FleetManagement.Modules.Fleet/Application/Commands/RegisterVehicle.cs)
- [RegisterDriver](../../src/Modules/Drivers/FleetCompany.FleetManagement.Modules.Drivers/Application/Commands/RegisterDriver.cs)
- [آزمون revision و اندازه‌گیری](../../tests/FleetCompany.FleetManagement.Integration.Tests/AvailabilityOptimizationTests.cs)
- [رقابت منابع و cache دو میزبان](../../tests/FleetCompany.FleetManagement.Integration.Tests/OperationsAcceptanceTests.cs)
