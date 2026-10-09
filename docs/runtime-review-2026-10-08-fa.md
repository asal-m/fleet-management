# گزارش اجرای واقعی و بررسی معماری

تاریخ: ۲۰۲۶/۱۰/۰۸. کد بررسی‌شده: `3a61cf094e1efb49566e8ee3238a948f412b29da`.

نتیجه: کد فعلی با Docker دوباره build و اجرا شد؛ در سناریوهای اجراشده خطای محصول مشاهده نشد. ساختار پروژه با مسیر بک‌اند MP Platform و پیکربندی انتخاب‌شدهٔ MP Core 0.9.3 سازگار است. این نتیجه تأیید تمام حالت‌های ممکن یا آمادگی کامل محیط عملیاتی نیست.

## اجرای تست‌ها

| بررسی | نتیجهٔ مشاهده‌شده |
|---|---|
| `docker compose up --build -d` | موفق؛ application و identity با image کد فعلی بازسازی شدند |
| Domain | ۹۱ موفق، صفر شکست، صفر skip |
| Application و معماری | ۳۱ موفق، صفر شکست، صفر skip |
| Integration با PostgreSQL و Redis واقعی | ۱۳ موفق، صفر شکست، صفر skip |
| مجموع تست‌های solution | ۱۳۵ موفق |
| بررسی مستقل HTTP روی سرویس زنده | ۷۰ درخواست با status مورد انتظار |
| SmokeClient | چهار Query کسب‌وکاری gRPC، چرخهٔ مأموریت، OIDC و Audit موفق |
| gRPC فنی | Probe، Health.Check و پاسخ اولیهٔ Health.Watch موفق؛ reflection بررسی شد |
| خرابی و بازیابی Redis | خواندن تازه از PostgreSQL در زمان قطع Redis موفق؛ readiness سالم؛ Redis دوباره روشن و تغییر وضعیت خودرو منعکس شد |

تست‌های Integration شامل هشت تخصیص رقیب با یک برنده، رقابت تخصیص/تعمیر، قواعد تخصیص، rollback، ابطال کش میان دو host، JWT با امضا و audience نامعتبر و هم‌بستگی trace هستند. این موارد از تست‌های موجود اجرا شدند؛ آزمایش بار گسترده انجام نشد.

## پوشش endpointهای REST

در هر ۱۷ endpoint کسب‌وکاری، بدون توکن پاسخ 401 و با نقش نامجاز پاسخ 403 بررسی شد. درخواست موفق نیز برای همه ارسال شد. 401 و 403 در این سناریوها نتیجهٔ صحیح‌اند.

| Method | مسیر | پاسخ موفق |
|---|---|---|
| POST | `/api/fleet/vehicles/` | 201 |
| GET | `/api/fleet/vehicles/{id}` | 200 |
| GET | `/api/fleet/vehicles/available` | 200 |
| PUT | `/api/fleet/vehicles/{id}/status` | 200 |
| POST | `/api/fleet/vehicles/{id}/maintenance/start` | 200 |
| POST | `/api/fleet/vehicles/{id}/maintenance/complete` | 200 |
| POST | `/api/drivers/` | 201 |
| GET | `/api/drivers/available` | 200 |
| POST | `/api/operations/missions/` | 201 |
| GET | `/api/operations/missions/{id}` | 200 |
| GET | `/api/operations/missions/active` | 200 |
| POST | `/api/operations/missions/{id}/schedule` | 200 |
| POST | `/api/operations/missions/{id}/assign` | 200 |
| POST | `/api/operations/missions/{id}/start` | 200 |
| POST | `/api/operations/missions/{id}/complete` | 200 |
| POST | `/api/operations/missions/{id}/cancel` | 200 |
| GET | `/api/administration/audit/` | 200 |

همچنین `/v1/platform/status` با توکن 200 و بدون توکن 401؛ هر سه مسیر `/health/live`، `/health/ready` و `/health/startup` برابر 200؛ سند `/openapi/v1.json` و UI برابر 200؛ `/metrics` بدون توکن 401 و با توکن 200 بودند.

چهار ورودی نامعتبر برای limit/page پاسخ 400، دو شناسهٔ ناموجود پاسخ 404، و لغو مأموریت تکمیل‌شده پاسخ 409 دادند. SmokeClient شروع تعمیر خودروی تخصیص‌یافته را با 409 و باقی‌ماندن Audit رد عملیات کنترل کرد.

جزئیات تمام ۷۰ درخواست در [endpoint-results-2026-10-08.json](endpoint-results-2026-10-08.json) ثبت شده است. توکن یا رمز در این فایل ثبت نشده است.

## پوشش gRPC

- `FleetVehicles.GetVehicle` و `GetAvailableVehicles`: موفق با JWT واقعی از OIDC fixture.
- `OperationsMissions.GetMission` و `GetActiveMissions`: موفق؛ فراخوانی مأموریت بدون توکن رد شد.
- `PlatformProbe.GetStatus`: پاسخ ready؛ بدون توکن Unauthenticated.
- `grpc.health.v1.Health.Check`: پاسخ SERVING.
- `grpc.health.v1.Health.Watch`: پاسخ اولیه SERVING؛ جریان پس از همین پاسخ بسته شد، تغییرات سلامت در طول زمان آزمایش نشد.
- reflection: از هر دو endpoint نسخهٔ v1 و v1alpha فهرست سرویس‌ها با موفقیت دریافت شد.
- دسترسی REST از listener اختصاصی gRPC برابر 404 بود.

## تطبیق با MP Platform / MP Core

مرجع معرفی‌شده، مسیر بک‌اند را به [MP Core](https://github.com/panahister/mpcore) ارجاع می‌دهد: [MP Platform](https://github.com/panahister/mp-platform). معیارهای بررسی از [معماری رسمی](https://github.com/panahister/mpcore/blob/main/docs/architecture/reference-architecture.md) و [مدل اجرای رسمی](https://github.com/panahister/mpcore/blob/main/docs/guide/concepts.md) گرفته شد.

| معیار | شواهد محلی | ارزیابی |
|---|---|---|
| مصرف framework از NuGet و نسخهٔ مشخص | `Directory.Build.props`، csprojها و manifest: 0.9.3، net10.0 | سازگار |
| شکل و transport طبق manifest | modular-monolith، both، messaging none، cache hybrid، audit postgresql | سازگار |
| مرز bounded context | Fleet، Drivers، Operations، Administration؛ ارجاع میان ماژول‌ها فقط به Contracts؛ ArchitectureTests موفق | سازگار |
| جدایی Domain/Application از EF و transport | بررسی معنایی Roslyn در ArchitectureTests موفق | سازگار |
| قواعد داخل aggregate | `CheckRule` و BusinessRuleهای نام‌دار؛ تست‌های Domain موفق | سازگار |
| commit متعلق به framework | handlerها IUnitOfWork دریافت می‌کنند؛ فراخوانی SaveChanges/Commit در ماژول‌ها یافت نشد؛ rollback در Integration بررسی شد | سازگار |
| امنیت پیش‌فرض و actor معتبر | bearer/OIDC، policy نقش‌ها، current actor چارچوب؛ درخواست‌های 401/403 و JWT نامعتبر در تست‌ها | سازگار |
| قرارداد خطا و زبان | Problem Details / gRPC ErrorInfo در تست‌ها؛ منابع انگلیسی/فارسی تمام کلیدها در guard معماری | سازگار در تست‌های اجراشده |
| Audit تغییر موفق و تلاش ردشده | SmokeClient هر دو را خواند؛ runtime role فقط SELECT/INSERT و فاقد UPDATE/DELETE/TRUNCATE بود | سازگار |
| کش و observability | Hybrid Cache، revision، تست ابطال میان hostها، آزمایش خرابی Redis و تست trace | سازگار در دامنهٔ آزمایش |
| عدم افزودن broker خارج از manifest | Kafka/RabbitMQ در deployment وجود ندارد؛ مسیر Publish/Send در ماژول‌ها یافت نشد | سازگار با messaging none |

انتخاب messaging none در راهنمای محلی MP Core به معنی نبود broker خارجی است؛ ذخیره‌سازی و صف محلی Wolverine همچنان وجود دارند. بنابراین D16 مغایرت اثبات‌شده با MP Core نیست، اما اگر چالش وجود هر نوع جدول Outbox/Inbox را ممنوع بداند، پذیرش آن موضوع باز است: [تصمیم D16](decisions/D16-framework-message-storage.md).

Createها در این نسخه Idempotency-Key ندارند؛ تکرار ساخت مأموریت می‌تواند رکورد تازه ایجاد کند. این محدودیت در معماری محلی مستند است و صرف وجود قابلیت idempotency در framework به معنی فعال‌بودن آن در همهٔ endpointها نیست.

## دامنه و وضعیت نهایی

اجرای حاضر از volumeهای موجود استفاده کرد؛ نصب از volume کاملاً خالی در این بررسی اجرا نشد. محیط OIDC توسعه‌ای تست شد؛ gateway/TLS، provider عملیاتی، backendهای telemetry خارجی و آزمون بار عملیاتی در دامنهٔ این اجرا نبودند.

هیچ کد محصول یا policy امنیتی تغییر نکرد. ابزارهای بررسی موقت در `.scratch` و این گزارش اضافه شدند. داده‌های ساختگی و Audit مربوط به بررسی باقی ماندند؛ خودروهای بررسی در پایان غیرفعال شدند. سرویس‌ها روشن مانده‌اند؛ PostgreSQL و Redis در بررسی نهایی healthy بودند. UI: http://localhost:8080/openapi-ui .
