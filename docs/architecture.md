# معماری Fleet Operations

نسخهٔ چارچوب: MP Core 0.9.3؛ Modular Monolith، REST + gRPC، Hybrid Cache،
Audit در PostgreSQL، Messaging/Time Series برابر None و AI tooling برابر Codex.

این سند رفتار پیاده‌شده را توضیح می‌دهد؛ جدول تصمیم‌های زیر مرجع طراحی فعلی است.
پیش‌تر بررسی شد که mpcore configure این فایل را مدیریت می‌کند؛ بنابراین
در تغییر تنظیمات، تعارض نسخهٔ ویرایش‌شده را بررسی کنید و سند محصول را کورکورانه
با فایل تولیدشده جایگزین نکنید.

## مرز ماژول‌ها و وابستگی‌ها

| ماژول | Aggregate و دادهٔ متعلق | رابط عمومی |
|---|---|---|
| Fleet | Vehicle؛ پلاک، نوع، ظرفیت، وضعیت پایه و تعمیر جاری | IFleetLookup در Contracts |
| Drivers | Driver؛ نام، وضعیت و مجموعهٔ صلاحیت‌ها | IDriverLookup در Contracts |
| Operations | Mission؛ چرخه، زمان و شناسهٔ منابع رزروشده | IResourceReservations در Contracts |
| Administration | endpoint خواندن Audit و مجوز Administrator؛ مالک نوشتن Audit نیست | IAuditQuery چارچوب |

API میزبان و Composition Root است. Application از Domain و پورت‌های محصول
استفاده می‌کند؛ Domain به HTTP، gRPC، EF، Redis یا ماژول داخلی دیگر وابسته نیست.
Infrastructure هر ماژول فقط نگاشت/پرس‌وجوی دادهٔ خودش را پیاده می‌کند.
AppDbContext مشترک در Infrastructure سراسری، نگاشت‌ها و سیاست Audit را ترکیب می‌کند.

ارجاع بین ماژول‌ها فقط به پروژه‌های Contracts است؛ Aggregate، IQueryable و
DbContext از مرز عمومی خارج نمی‌شوند. Shared Contracts فقط پورت فنی هماهنگی و
کش دارد، نه مدل کسب‌وکار مشترک. نیاز به Contracts مستقل برای جلوگیری از cycle
بین پروژه‌های اصلی است، نه نیاز به سرویس یا دیتابیس جدا.

## تصمیم‌ها و trade-offها

مرزهای وابستگی با ArchitectureTests محافظت می‌شوند: ارجاع میان ماژول‌ها فقط
به Contracts و استفاده‌نکردن Domain/Application از EF، ASP.NET، gRPC یا broker.
تست ترجمه نیز متن غیرخالی انگلیسی و فارسی تمام کلیدهای قواعد، validation و failure
را بررسی می‌کند. این تست‌ها source را با Roslyn تحلیل می‌کنند و checkout لازم دارند.

| شناسه | رفتار پیاده‌شده و دلیل | هزینه یا وضعیت |
|---|---|---|
| D03 | manifest: codex؛ workflow واقعی در یادداشت AI | حل‌شده |
| D04 | ظرفیت/بار: کیلوگرم صحیح مثبت؛ کد نوع قابل گسترش، نه enum ثابت؛ صلاحیت تطبیق دقیق کد نهایی | کد نوع، کاتالوگ رسمی یا تأیید صلاحیت قانونی ایجاد نمی‌کند |
| D05 | رزرو انحصاری تا پایان یا لغو؛ فقط Assigned/InProgress منابع را می‌گیرند | بازهٔ زمان/مدت اضافه نشده؛ مأموریت‌های آینده نمی‌توانند هم‌زمان همان منبع را رزرو کنند |
| D06 | availability = وضعیت Fleet + نبود رزرو Operations از Contracts فقط‌خواندنی | یک Query می‌تواند داده را لحظه‌ای ببیند؛ Assign همیشه منبع واقعی را دوباره بررسی می‌کند |
| D07 | Draft → Scheduled → Assigned → InProgress → Completed؛ Cancel فقط پیش از شروع | وضعیت نهایی احیا نمی‌شود؛ زمان UTC با دقت میکروثانیه |
| D08 | Active/Inactive وضعیت پایه؛ تعمیر جدا روی Vehicle و وضعیت مؤثر UnderMaintenance | Maintenance داخل Vehicle است چون قواعد شروع/پایان/وضعیت باید یکجا سازگار باشند؛ تاریخچهٔ تعمیر Aggregate جدا ندارد |
| D09 | تعمیر یا Inactive کردن خودروی رزروشده رد می‌شود؛ Scheduled بدون منابع مانع نیست | لغو خودکار مأموریت وجود ندارد؛ rejection بدون تغییر کسب‌وکار، همراه Audit |
| D10 | یک Aggregate کسب‌وکار در هر تراکنش؛ منابع با شناسه و خواندن Contracts، نه نوشتن Aggregate ماژول دیگر | رابط نوشتن بین ماژول‌ها و پیام رزرو استفاده نشده؛ شمارندهٔ فنی و Audit با همان commit همراه‌اند |
| D11 | قفل advisory برای هر Mission و هر Driver/Vehicle با ترتیب ثابت + unique index شرطی رزرو | منابع مستقل می‌توانند هم‌زمان تغییر کنند؛ شمارندهٔ کش هنوز مشترک است؛ جزئیات [D21](decisions/D21-resource-locks-and-readable-rest-states.md) |
| D12 | MP Core Hybrid Cache؛ کلید نسخه‌دار و TTL=30s؛ بازبینی نسخه پس از cache read | هر Query دو خواندن کوچک نسخه از DB دارد؛ availability snapshot مرجع تصمیم Assign نیست |
| D13 | JWT/OIDC واقعی؛ Operator، FleetManager، Administrator؛ حساب محدود DB برای Audit | ابزار جدا و کوچک OIDC فقط Development؛ محیط عملیاتی نیازمند provider واقعی و TLS است |
| D14 | REST برای نوشتن و خواندن؛ چهار Query gRPC از همان Application | نگاشت DTO/proto در Transport؛ قواعد کسب‌وکار تکرار نمی‌شوند |
| D15 | Domain/Application/Integration؛ ۱۳ سناریوی الزامی با دادهٔ مصنوعی مستقل | تست وابسته بدون تنظیم fixture skip می‌شود؛ اجرای کامل از script مستند است |
| D16 | حضور ذخیره‌سازی داخلی Wolverine حتی با messaging none تأیید شده؛ [شاهد API و source](decisions/D16-framework-message-storage.md) | **باز؛ نیازمند تأیید** انطباق با ممنوعیت Outbox/Inbox چالش |

### قواعد و تکرارپذیری

Value Objectها قالب و یکسان‌سازی را اعمال می‌کنند: نوع/صلاحیت به حروف بزرگ
انگلیسی و A–Z/0–9/_ با طول ۱..۵۰؛ پلاک trim، حروف بزرگ و رقم فارسی/عربی
به انگلیسی، طول ۱..۳۰ و یکتا؛ نام ۱..۱۰۰ و محل ۱..۵۰۰ پس از trim.
حداقل یک صلاحیت معتبر لازم است؛ تکراری‌ها پس از اعتبارسنجی حذف می‌شوند.

Mission قواعد وضعیت، ظرفیت، فعال بودن، تعمیر، رزرو و صلاحیت را با BusinessRuleهای
نام‌دار قبل از تغییر اعمال می‌کند. Vehicle حقیقت رزرو را از Application دریافت
و در Domain، تعمیر/غیرفعال‌سازی غیرمجاز را رد می‌کند. Application حقایق قابل
اعتماد منابع را در قفل مشترک می‌خواند؛ Transport اجازهٔ ارسال آن‌ها را نمی‌دهد.

Assign تکراری با همان دو شناسه فقط در Assigned بدون تغییر است؛ Start در
InProgress، Complete در Completed و Cancel در Cancelled نیز بدون تغییرند.
برای تکرار مجاز Audit موفق و افزایش نسخه دوباره رخ نمی‌دهد. Create عملیات
جدید است؛ idempotency key برای Create در این نسخه تعریف نشده است.
خطای commit ممکن است پاسخ را مبهم کند؛ خواندن وضعیت و تکرار مجاز مسیرهای چرخه
برای رفع ابهام مناسب‌اند، ولی ایجاد دوبارهٔ Mission چنین تضمینی ندارد.

### سیاست تعمیر و مأموریت

مالک رزرو، Operations است؛ فقط Mission با وضعیت Assigned یا InProgress، خودرو و
راننده را رزرو می‌کند. زمان ScheduledTime بازهٔ رزرو نیست: رزرو از Assign تا
Complete یا Cancel ادامه دارد، حتی اگر مأموریت برای هفتهٔ بعد باشد. مأموریت
Scheduled هنوز خودرو و راننده ندارد. شروع مأموریت در مدل فعلی به رسیدن زمان
ScheduledTime وابسته نیست؛ این زمان دادهٔ برنامه‌ریزی است.

| وضعیت پیش از درخواست | درخواست | رفتار فعلی و دلیل |
|---|---|---|
| خودرو دارای Mission در Assigned | Start Maintenance | رد با VEHICLE_HAS_ACTIVE_MISSION؛ منبع تعهدشده نباید هم‌زمان وارد تعمیر شود |
| خودرو دارای Mission در InProgress | Start Maintenance یا Inactive | رد با همان قاعده؛ Mission و Vehicle بدون تغییر می‌مانند |
| Mission فقط Scheduled؛ خودرو هنوز تخصیص ندارد | Start Maintenance خودرو آزاد | مجاز، اگر سایر قواعد Vehicle برقرار باشند؛ Scheduled منبعی رزرو نکرده است |
| خودرو تحت تعمیر؛ Mission در Scheduled | Assign همان خودرو | رد با VEHICLE_UNDER_MAINTENANCE؛ eligibility زیر قفل مشترک دوباره خوانده می‌شود |
| Mission به Completed یا Cancelled رسیده | Start Maintenance خودرو قبلی | مجاز، اگر رزرو فعال دیگری نباشد؛ IDهای تاریخی روی Mission رزرو فعال محسوب نمی‌شوند |
| درخواست Assign و Start Maintenance هم‌زمان | رقابت روی یک خودرو | هر دو نمی‌توانند موفق شوند؛ قفل تراکنشی مشترک بررسی و تغییر را هماهنگ می‌کند |

رد درخواست تعمیر، تخصیص را حذف یا مأموریت را خودکار لغو نمی‌کند. سیاست انتخابی
تعهد منابع را حفظ می‌کند؛ عملیات ردشده جدا از تراکنش در Audit ثبت می‌شود تا
rollback تاریخچهٔ تلاش را از بین نبرد. در قدم سوم، تعارض رزرو همچنان HTTP 409
است؛ خودرو تحت تعمیر یا سایر قواعد eligibility و lifecycle، HTTP 422 می‌دهند.
منبع ناموجود هنگام Assign، HTTP 404 دارد. اگر در رقابت تعمیر برنده شود، Assign
به دلیل VEHICLE_UNDER_MAINTENANCE با 422 رد می‌شود؛ اگر Assign برنده شود، تعمیر
به دلیل VEHICLE_HAS_ACTIVE_MISSION با 409 رد می‌شود. سیاست کسب‌وکار تغییر نکرده است.

گزینهٔ جایگزین حذف خودکار تخصیص، نیازمند تعریف وضعیت جدید مأموریت و مسئولیت
اپراتور است؛ تعمیر اضطراری همراه توقف مأموریت نیازمند workflow کسب‌وکاری تازه
است. این گزینه‌ها در مدل فعلی فعال نیستند و تغییر آن‌ها تصمیم مالک محصول است.

شواهد کد: Vehicle.StartMaintenance / ChangeBaseStatus، Mission.HasActiveReservation
و MissionWorkflow.Assign. شواهد تست: VehicleReservationTests و
OperationsAcceptanceTests برای رد تعمیر منبع رزروشده و رقابت تخصیص/تعمیر.
قدم اول فقط مستندات را اصلاح کرد و تست‌ها را دوباره اجرا نکرد. در قدم سوم،
این رفتار و statusهای اصلاح‌شده در مجموعهٔ کامل با صفر skip تأیید شدند؛
جزئیات در [شاهد قدم سوم](verification-step-3-fa.md) ثبت شده‌اند.

### تراکنش و هم‌زمانی

جزئیات سیاست رزرو، نقش ScheduledTime، محدودیت available و تصمیم‌های لازم برای
مدل بازه‌ای در [D17](decisions/D17-reservation-and-schedule-policy.md) آمده است.

Handler مالک SaveChanges/Commit نیست؛ IUnitOfWork، middleware تراکنشی چارچوب
را انتخاب می‌کند. قفل pg_advisory_xact_lock تا commit/rollback نگه داشته می‌شود.
به‌این‌ترتیب Assign یک Mission را تغییر می‌دهد، شروع تعمیر یک Vehicle را؛ دو
Aggregate متفاوت در تراکنش یکدیگر نوشته نمی‌شوند. شمارندهٔ availability و
رکورد Audit زیرساخت فنی همراه commit هستند.

فیلتر index یکتایی در operations.missions، Status IN (3,4) است؛ خودرو و
راننده هرکدام آخرین سد یکتایی دارند. تمام مسیرهای محصول که availability را
تغییر می‌دهند باید پروتکل قفل را رعایت کنند. نوشتن مستقیم SQL خارج از این
پروتکل تضمین رقابت با تعمیر را ندارد.

گزینهٔ جایگزین optimistic concurrency نیازمند version روی منبع مشترک و retry
است؛ version هر Mission به‌تنهایی دو Mission را هماهنگ نمی‌کند. قفل ردیف/منبع
با ترتیب ثابت concurrency بیشتری می‌دهد؛ این راه در D21 پیاده شده و شناسهٔ قفل و ترتیب گرفتن منابع در آن توضیح داده شده است.
در مدل رزرو انحصاری unique index کافی است؛ Exclusion زمانی فقط با مدل بازه‌ای
معنی دارد. در همهٔ گزینه‌ها باید یک Aggregate کسب‌وکار حفظ یا استثنا مستند شود.

### کش

کلید: fleet:available:v1:{revision}:limit:{limit}؛ تمام فیلتر موجود در کلید است.
revision سراسری PostgreSQL با تغییر مرتبط در همان تراکنش افزایش می‌یابد؛
تغییر مرتبط یعنی تغییر عضویت خودروهای available: ثبت راننده، Schedule و Start
آن را افزایش نمی‌دهند؛ تغییر وضعیت/تعمیر فقط هنگام تغییر واجدشرایط‌بودن خودرو
اثر دارد. جدول کامل commandها و علت حفظ قفل منابع در
[D18](decisions/D18-availability-revision-and-coordination.md) است.
پس از commit همهٔ instanceها کلید جدید را می‌خوانند. کلید قدیمی در ۳۰ ثانیه
منقضی می‌شود؛ tag invalidation فرض نشده است. اگر نسخه طی cache read عوض شود،
پاسخ از DB تازه خوانده می‌شود. race پس از آخرین خواندن همیشه برای یک snapshot
ممکن است؛ Assign از cache استفاده نمی‌کند و زیر قفل دوباره اعتبار می‌سنجد.

Redis failure: استفاده از DB به‌عنوان fallback و log بدون credential؛ cache
برای صحت نوشتن لازم نیست. خرابی DB به پاسخ قدیمی قابل تخصیص تبدیل نمی‌شود.
قطع/وصل Redis با script مستقل قابل آزمایش است؛ دو instance در تست invalidation
استفاده شده‌اند. شمارنده سراسری، invalidation ساده با هزینهٔ ابطال گسترده است.

### Audit و امنیت

سیاست Audit در Infrastructure مشترک فقط Status/ScheduledTime/شناسهٔ منابع
Mission و BaseStatus/IsUnderMaintenance خودرو را ثبت می‌کند؛ نام، آدرس و پلاک
به change payload اضافه نشده‌اند. Entity audit خودکار چارچوب است؛ عملیات
معنادار با IBusinessAuditRecorder ثبت می‌شوند. موفقیت با commit، رد قاعده با
RecordAttemptAsync جدا از تراکنش ثبت می‌شود. Actor از توکن اعتبارسنجی‌شده می‌آید.

Administrator فقط از endpoint خواندن مجاز استفاده می‌کند؛ هیچ endpoint تغییر
یا حذف Audit وجود ندارد. Runtime DB role مجوز UPDATE/DELETE/TRUNCATE Audit ندارد.
Application bearer-only است؛ ابزار DevelopmentIdentity بیرون آن، کلید RSA
در volume و توکن پنج‌دقیقه‌ای دارد. Keycloak اضافه نشده است.

### راه‌اندازی محلی

Compose ابتدا bootstrap را اجرا می‌کند؛ رمزهای تصادفی مالک، runtime و Redis در
volume تنظیمات با فایل‌های 0600 ذخیره می‌شوند. اصل تنظیمات قبل از فایل‌های مشتق
ثبت می‌شود تا اجرای مجدد پس از قطع، رمز دیتابیس را عوض نکند. خرابی اصل تنظیمات
باعث توقف می‌شود؛ رمز تازه برای دیتابیس موجود تولید نمی‌شود. واردکردن رمز نصب
قدیمی فقط هنگام خالی‌بودن volume و با تمام مقادیر لازم انجام می‌شود.
برنامه فایل mounted را فقط در Development می‌خواند و environment/command line
بر آن اولویت دارند. volume دیتابیس و تنظیمات باید با هم نگهداری شوند.
Redis cache موقت است و به فایل persistence نیاز ندارد.

### Observability و بررسی

Foundation MP Core و OpenTelemetry، به‌همراه ActivitySource محصول و Npgsql،
درخواست را از Transport به Application، اجرای عملیات Domain و Infrastructure دنبال می‌کنند.
Activity مربوط به فراخوانی Domain در Application ساخته می‌شود؛ aggregate ابزار tracing ندارد.
X-Trace-Id و scopeهای trace/span در log برای ارتباط مناسب‌اند. نام/آدرس/پلاک
در ToString پیام‌ها توسعه داده نمی‌شوند؛ SQL sensitive-data logging فعال نیست.
Audit حقیقت ماندگار کسب‌وکار است؛ log جزئیات عیب‌یابی با سیاست نگهداری متفاوت.

شاهد تست‌ها و محدودیت‌ها در [verification.md](verification.md) ثبت می‌شود.
Framework source تغییر نکرده؛ Kafka/RabbitMQ/TimeSeries/Microservice یا workflow
Outbox/Inbox کسب‌وکار اضافه نشده است. D16 را نمی‌توان با نبود Publish به‌تنهایی
بسته دانست.

مالکیت داده و پورت‌ها، چرخهٔ مفهومی و علت shared kernel فنی در
[D19](decisions/D19-module-ownership-and-read-contracts.md) آمده‌اند. ماتریس عملیات
مجاز هر وضعیت Mission و سیاست کدهای آزاد در
[D20](decisions/D20-lifecycle-and-extensible-codes.md) ثبت شده‌اند.
