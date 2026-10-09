# قدم ششم؛ مرز ماژول‌ها، ورودی مشترک و نگاشت وضعیت

تاریخ: ۲۰۲۶-۱۰-۰۹. شناسهٔ اجرای نهایی: `05856ed180cf43c09473830b954302ac`.
اجرای کامل پیش از تقویت پوشش status نیز با شناسهٔ `8137bc5c758649108fd1d0d91eac5bf3`
هر ۱۷۲ تست را پاس کرد؛ شمار تست‌ها در اجرای نهایی ثابت است و assertionهای بیشتری دارد.

## تغییرها و دلیل آن‌ها

1. **M5؛ مسیر حداقلی مستندسازی:** [D19](decisions/D19-module-ownership-and-read-contracts.md)
   مالک داده، پورت‌های فقط‌خواندنی، چرخهٔ مفهومی و دلیل یک Context و shared kernel
   فنی را توضیح می‌دهد. تست معماری همچنان ارجاع به main project ماژول دیگر و
   وابستگی EF/Transport در Domain/Application را رد می‌کند. بازطراحی مالکیت انجام نشده است.
2. **m1؛ اصلاح بخش Fleet:** StartMaintenance، CompleteMaintenance و ChangeVehicleStatus
   عملیات صریح خود را به مکانیزم مشترک lock/load/Audit/revision می‌دهند. نام Audit
   دیگر تعیین‌کنندهٔ رفتار نیست. انتقال کامل orchestration مأموریت از MissionWorkflow
   به handlerهای مستقل هنوز باز است؛ m1 کامل بسته نمی‌شود.
3. **m2:** Activityها از aggregate مأموریت به Application منتقل شدند. نام spanها حفظ شد؛
   تست واقعی trace همچنان ارتباط Transport، Application، عملیات Domain و Infrastructure
   را بررسی می‌کند. محافظ معماری استفادهٔ System.Diagnostics در Domain را رد می‌کند.
4. **m3:** snapshot و Query وضعیت واقعی خودرو را می‌خوانند؛ دو متد gRPC از یک mapper
   استفاده می‌کنند. تست موجود با گذار واقعی Inactive، تعمیر Inactive، تعمیر Active
   و پایان تعمیر گسترش یافت و REST/gRPC را در هر مرحله مقایسه کرد. فیلتر availability
   همچنان فقط خودروهای واجد شرایط را برمی‌گرداند؛ سیاست eligibility تغییر نکرد.
5. **m4:** parse زمان از REST به Application منتقل شد. validator و guard مستقیم handler
   زمان null، نامعتبر یا بدون offset را رد می‌کنند؛ Z و offset صریح پذیرفته می‌شوند.
   شرط زمان آینده در Domain باقی است. REST همچنان scheduledTime رشته‌ای می‌گیرد؛
   مدل داخلی ScheduleMission اکنون رشته می‌گیرد تا فراخوانی مستقیم همان اعتبارسنجی را داشته باشد.
6. **m12:** ثبت مستقیم خودرو/راننده با وضعیت null یا خارج enum، FailureDescriptor از
   نوع Validation برمی‌گرداند و چیزی stage نمی‌کند. دیگر به ArgumentNullException
   متکی نیست. مسیر HTTP همچنان validator چارچوب و Problem Details دارد. تست‌های HTTP
   وضعیت حذف‌شده، null و خارج enum را برای هر دو ثبت صریح بررسی کردند؛ پاسخ 400،
   کد status و نبود رکورد کسب‌وکار تأیید شدند.
7. **m7/m8:** [D20](decisions/D20-lifecycle-and-extensible-codes.md) ماتریس چرخه، محدودیت
   Cancel/Reassign/Reschedule و سیاست کدهای آزاد را ثبت می‌کند. کاتالوگ جدید یا
   قاعدهٔ کسب‌وکار تازه اضافه نشده است؛ انتخاب کاتالوگ همچنان تصمیم محصول است.
8. **M6؛ اطلاعات جزئی:** نام تأییدشدهٔ **Asal Mozafari** در
   [سند نویسندگی](authorship-fa.md) ثبت شد. علت تفاوت identity و تاریخچهٔ یک commit
   هنوز توضیح داده نشده؛ M6 باز است.

## نتیجهٔ واقعی آزمون‌ها

| پروژه | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Domain | 91 | 0 | 0 |
| Application | 44 | 0 | 0 |
| Integration | 37 | 0 | 0 |
| مجموع | **172** | **0** | **0** |

پیش از اجرای کامل، تست‌های جدید Application خطایی واقعی در FieldViolation پیدا کردند:
مسیر `baseStatus` با قرارداد lower_snake_case چارچوب سازگار نبود. اجرای اولیه
۴۲ Passed و ۲ Failed داشت. مسیر به `base_status` اصلاح شد و اجرای کامل بالا پاس شد؛
assertion حذف یا ضعیف نشد. build پیش از افزودن تست‌ها نیز صفر warning/error داشت.

۱۲ مورد به شمار قبلی ۱۶۰ اضافه شدند: چهار مورد وضعیت نامعتبر در handlerهای ثبت،
هفت مورد زمان Schedule و یک تست REST ورودی نامعتبر. تست gRPC موجود گسترش یافت.
ورودی نامعتبر REST پاسخ 400 داد؛ Mission در Draft و زمان null ماند و هیچ
MissionScheduled Audit ایجاد نشد. تست‌های رقابت منابع، rollback، Audit، JWT/roleها،
کش چندمیزبانی و trace موجود نیز در اجرای کامل پاس شدند.

اعتبارسنج سخت‌گیر TRX هر سه assembly، Counters، تعریف و نتیجهٔ یکتای تک‌تک تست‌ها
و Passed بودن همهٔ آن‌ها را کنترل کرد. گزارش‌های خام محلی در
`.scratch/full-suite/05856ed180cf43c09473830b954302ac/results` نگهداری شدند.

## محیط و حدود شاهد

PostgreSQL/Redis و سایر وابستگی‌ها با volumeهای تازه و پورت تصادفی راه افتادند.
bootstrap، migration و readiness پاس شد. آزمون‌های HTTP/gRPC با TestServer از source
فعلی محصول روی میزبان build شدند و JWT واقعی fixture را اعتبارسنجی کردند.
imageهای bootstrap/identity/application برای راه‌اندازی و readiness از قدم سوم بودند؛
این اجرا ساخت image جدید محصول یا موفقیت build سرد را اثبات نمی‌کند.

منابع اختصاصی اجرا و builder دارای cache واقعی در finally پاک شدند؛ exit code صفر بود.
ID هر ۱۶ container و نام هر ۱۷ volume قبلی با snapshot پیشین برابر ماند؛ builderهای
default/desktop-linux حفظ شدند و image tag اختصاصی اجرا باقی نماند. فضای آزاد C:
پس از اجرا 13.29 GB بود. prune سراسری یا حذف volumeهای توسعه انجام نشد.
سرویس‌های اصلی توسعه به درخواست این قدم راه‌اندازی نمی‌شوند.

کنترل تحویل: ۴۸ فایل Markdown و ۱۳۹ لینک محلی بدون مقصد مفقود بررسی شدند؛
`git diff --cached --check` موفق بود. اصلاحات این قدم staged هستند؛ فایل‌های
Postman از قبل untracked باقی ماندند. خروجی تست و credential وارد index نشده‌اند.

این قدم پذیرش نهایی همهٔ ۲۱ ایراد نیست. Linux CI/build سرد، M7/m11، m6، بازطراحی
کامل m1، بهبود قرارداد m5، formatting گسترده m9، collector/metrics m10، تکمیل M6
و تصمیم D16 همچنان کار یا شاهد جدا نیاز دارند. commit/push انجام نشده است.
