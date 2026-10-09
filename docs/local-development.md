# محیط محلی Fleet

از ریشهٔ پروژه اجرا کنید:

```powershell
docker compose up --build
```

برای نصب تازه، فقط Docker با Linux containers و Compose لازم است؛ نصب محلی
PostgreSQL، Redis، .NET یا EF CLI و اجرای اسکریپت آماده‌سازی لازم نیست.
سرویس bootstrap رمزهای تصادفی را در volume اختصاصی Docker می‌سازد. برنامه فایل
تنظیمات توسعه را به‌صورت فقط‌خواندنی دریافت می‌کند؛ migration با حساب مالک اجرا
و حساب محدود runtime جداگانه آماده می‌شود. رمزها در repository یا خروجی چاپ نمی‌شوند.

پورت‌ها: REST روی 8080، gRPC روی 8081، هویت توسعه روی 55480، PostgreSQL روی
55432 و Redis روی 56379؛ همگی فقط روی loopback منتشر می‌شوند.

## نصب موجود با User Secrets

اگر دیتابیس نسخهٔ قبلی را دارید، قبل از اولین اجرای نسخهٔ جدید یک بار اجرا کنید:

```powershell
.\scripts\Initialize-ComposeEnvironment.ps1
docker compose up --build
```

bootstrap رمزهای موجود را فقط هنگام خالی بودن volume تنظیمات وارد می‌کند.
پس از آن، ترمینال تازه هم با دستور مستقیم Compose کار می‌کند. volume دیتابیس
حذف یا تعویض نمی‌شود. حذف volume تنظیمات در حالی که دیتابیس باقی مانده است
به معنی از دست دادن تنظیمات اتصال است؛ این دو باید با هم backup/restore شوند.
workflow جدید اجرای host، محیط مستقل دارد و پورت/volume محیط کامل را استفاده نمی‌کند.

## اجرای API روی میزبان با وابستگی مستقل

پیش‌نیاز: .NET 10 SDK، Docker با Linux containers، Compose حداقل 2.24.4 و دسترسی
به feed بسته‌های پروژه. از ریشهٔ پروژه، در Windows/Linux/macOS:

```sh
dotnet run --project tools/LocalDevelopment --configuration Release -- run
```

ابزار C# به shell یا executable با پسوند exe وابسته نیست؛ Docker و dotnet باید
روی PATH باشند. wrapper اختیاری PowerShell همان ابزار را صدا می‌زند. اجرای واقعی
روی Windows بررسی می‌شود؛ اجرای native Linux/macOS شاهد جدا نیاز دارد.

| مورد | محیط کامل Docker | وابستگی‌های API روی میزبان |
|---|---|---|
| Compose project | fleetmanagement-local | fleetmanagement-dependencies |
| REST / gRPC | 8080 / 8081 | 8180 / 8181، فقط loopback |
| PostgreSQL / Redis | 55432 / 56379 | 56432 / 57379 |
| Identity fixture | 55480 | 56480 |
| volumeها | با پیشوند محیط کامل | پیشوند مستقل، دیتابیس/تنظیمات/کلید جدا |

bootstrap موجود بدون کپی قواعد یا secret، تنظیمات مستقل را در volume خصوصی می‌سازد.
ابزار آن را در حافظه می‌خواند و فقط environment فرزند API را تنظیم می‌کند. User
Secrets قبلی عوض نمی‌شوند. Migration با owner و runtime با حساب محدود اجرا می‌شوند؛
JWT/نقش‌ها همچنان اعتبارسنجی می‌شوند. appsettings.Development.json حامل secret
نیاز نیست؛ ابزار تنظیمات Development را صریح تأمین می‌کند. Production از این ابزار
یا fixture استفاده نمی‌کند و به تنظیمات خارجی و OIDC واقعی نیاز دارد.

```sh
dotnet run --project tools/LocalDevelopment --configuration Release -- verify
dotnet run --project tools/LocalDevelopment --configuration Release -- down
```

verify قبل از شروع API همهٔ پروژه‌ها و SmokeClient را build می‌کند؛ سپس readiness،
smoke واقعی REST/gRPC و کل suite با TRX سخت‌گیر را اجرا می‌کند. روی Windows این
ترتیب از کپی DLL قفل‌شده جلوگیری می‌کند. verify رکوردهای ساختگی و Audit در دیتابیس
توسعه باقی می‌گذارد. نتیجه‌ها در `.scratch/local-development/<run-id>/results` هستند.
host موقت در finally متوقف می‌شود؛ dependencyها برای بررسی می‌مانند. down و Ctrl+C
داده/رمزها را حذف نمی‌کنند؛ حذف volumeهای دیتابیس موجود بخشی از این workflow نیست.

متغیرهای اختیاری پورت: `FLEET_DEPENDENCIES_POSTGRES_PORT`،
`FLEET_DEPENDENCIES_REDIS_PORT`، `FLEET_DEPENDENCIES_IDENTITY_PORT`،
`FLEET_LOCAL_REST_PORT` و `FLEET_LOCAL_GRPC_PORT`. باید عدد معتبر و متمایز باشند.
Compose و host از همان مقدار استفاده می‌کنند؛ issuer/JWKS fixture به پورت identity
تطبیق می‌یابد. `--project fleetmanagement-dependencies-<suffix>` نصب مستقل می‌سازد؛
نام پروژهٔ کامل پذیرفته نمی‌شود. `--no-build` فقط build تصاویر Docker را حذف می‌کند؛
source میزبان همچنان قبل از اجرا build می‌شود. با نام suffix برای down همان نام را بدهید.

## داده‌های workflow قدیمی dependencies

فایل قدیمی از volume `fleetmanagement-local_fleet-postgres` استفاده می‌کرد. فایل جدید
آن را جابه‌جا/حذف/reset نمی‌کند؛ نصب جدید دیتابیس مستقلی دارد. برای مشاهدهٔ دادهٔ
قدیمی از محیط کامل با همان volume و import اولیهٔ User Secrets قبلی استفاده کنید.
اگر انتقال به دیتابیس مستقل لازم شد، backup/restore صریح و نگهداری credentials
لازم است؛ این قدم مهاجرت خودکار داده انجام نمی‌دهد. تنظیمات اتصال User Secrets
قدیمی را هم تغییر نمی‌دهد. `Initialize-ComposeEnvironment.ps1` فقط برای import
اعتبارنامه‌های نصب قدیمی است و برای workflow تازه لازم نیست.

## تست و بررسی

پس از اجرای Compose، برای suite کامل با .NET 10 SDK و PowerShell:

```powershell
.\scripts\Test-ComposeEnvironment.ps1
.\scripts\Verify-ComposeSmoke.ps1
.\scripts\Verify-RedisFailure.ps1
```

اسکریپت تست، تنظیمات تولیدشده را فقط در حافظه می‌خواند و environment لازم برای
host و fixture را یکجا تنظیم می‌کند؛ سپس مقادیر قبلی را بازمی‌گرداند. اجرای سادهٔ
dotnet test بدون fixture با خطای FLEETTEST001 متوقف می‌شود؛ برای اجرای کامل، از فرمان verify استفاده کنید.
تست قطع Redis را هم‌زمان با suite اجرا نکنید.

```powershell
docker compose ps
docker compose down
```

down بدون گزینهٔ حذف volume داده‌ها و رمزها را نگه می‌دارد. Redis فقط cache است و
فایل snapshot/AOF ندارد. هویت محلی صرفاً Development است؛ backend همچنان token
معتبر و نقش مناسب می‌خواهد. برای Production از OIDC واقعی، TLS و مدیریت مستقل
رمزها و migration استفاده کنید.

یادداشت‌های تاریخی workflow قبلی صرفاً محلی نگه داشته شده‌اند؛ راهنمای اجرای
نسخهٔ تحویلی همین سند و README است. وضعیت پذیرش ذخیره‌سازی داخلی چارچوب در
[D16](decisions/D16-framework-message-storage.md) آمده است.
