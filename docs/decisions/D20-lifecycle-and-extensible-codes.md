# D20؛ چرخهٔ مأموریت و کدهای قابل گسترش (m7/m8)

تاریخ: ۲۰۲۶-۱۰-۰۹. سیاست موجود مستند می‌شود؛ transition یا کاتالوگ جدید ساخته نمی‌شود.

کاربر در همین تاریخ حفظ کدهای قابل گسترش و مستندسازی محدودیت را انتخاب کرد؛
فهرست محدود ساختگی یا تصحیح خودکار املای کد ایجاد نمی‌شود. تصمیم افزودن بازهٔ
زمانی مستقل از این سیاست است و قواعد حالت بازه‌ای هنوز نیازمند تعیین‌اند.

| وضعیت فعلی | Schedule | Assign | Start | Complete | Cancel |
|---|---|---|---|---|---|
| Draft | Scheduled | رد | رد | رد | Cancelled |
| Scheduled | Scheduled؛ زمان آیندهٔ تازه مجاز | Assigned | رد | رد | Cancelled |
| Assigned | رد | همان منابع: بدون تغییر؛ منابع دیگر: رد | InProgress | رد | Cancelled |
| InProgress | رد | رد | بدون تغییر | Completed | رد |
| Completed | رد | رد | رد | بدون تغییر | رد |
| Cancelled | رد | رد | رد | رد | بدون تغییر |

Schedule با زمان عیناً یکسان و precision میکروثانیه بدون تغییر است، اما قاعدهٔ
آینده‌بودن همچنان بررسی می‌شود؛ retry بعد از گذشته‌شدن آن زمان تضمین no-op ندارد.
Assign تکراری فقط در Assigned با همان دو ID مجاز است؛ پس از Start دیگر retry
Assign مجاز نیست. Create idempotency key ندارد و دو Create می‌توانند دو Mission بسازند.
تکرارهای مجاز، Audit موفق یا revision جدید نمی‌سازند. شکست‌ها قرارداد failure و
Audit تلاش موجود را دارند. سیاست زمان و رزرو فوری در D17 است.

Abort پس از Start، تعویض منابع Assigned و reschedule پس از Assign گزینه‌های
آینده‌اند و نیازمند تصمیم محصول دربارهٔ آزادسازی، Audit و تخصیص دوباره هستند.
هیچ‌کدام در این اصلاح مستندات اضافه نشده‌اند.

## نوع خودرو و qualification

ورودی trim و به uppercase نرمال می‌شود؛ کد ۱ تا ۵۰ نویسه از A–Z، 0–9 یا `_`
است. تطبیق qualification با نوع خودرو دقیق و ordinal روی کد نرمال‌شده است.
`truck` به `TRUCK` تبدیل می‌شود؛ `TRUK` از نظر syntax معتبر است، اما معادل
`TRUCK` نیست. این سیستم صحت املایی یا صلاحیت قانونی را از این کد استنتاج نمی‌کند.

مالک کد نوع Fleet و مالک فهرست qualification، Drivers است؛ Operations فقط
snapshot آن‌ها را برای قاعدهٔ تخصیص مصرف می‌کند. کاتالوگ رسمی یا seed محدود
ساختگی نداریم. کاتالوگ آینده باید مالک، روند افزودن/حذف کد و رفتار unknown code
و migration را با تصمیم محصول مشخص کند.

شواهد: [Mission](../../src/Modules/Operations/FleetCompany.FleetManagement.Modules.Operations/Domain/Missions/Mission.cs)،
[آزمون Domain](../../tests/FleetCompany.FleetManagement.Domain.Tests/MissionTests.cs)،
[آزمون صلاحیت](../../tests/FleetCompany.FleetManagement.Domain.Tests/DriverTests.cs)
و [سیاست زمان](D17-reservation-and-schedule-policy.md).
