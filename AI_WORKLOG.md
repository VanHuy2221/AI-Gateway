# AI_WORKLOG

Ghi lại quá trình dùng AI để xây dựng AI Gateway API, gồm công cụ đã dùng, prompt tiêu biểu, các lỗi AI đưa ra và cách kiểm chứng/sửa lại.

## 1. Công cụ AI đã sử dụng

| Công cụ | Vai trò trong dự án |
|---|---|
| **Claude** (Anthropic) | Lập kế hoạch dự án, dựng khung Web API, database (EF Core + SQL Server), JWT authentication, nối conversation storage + RequestLog + retry/timeout + rate limiting + usage endpoint, Docker + deploy, viết tài liệu (README, AI_WORKLOG, Postman) |
| **ChatGPT** | Bản đầu tiên của việc tích hợp Gemini API (`AiController`, `AiService` gọi `/ai/chat`) |

## 2. AI đã giúp gì (theo mốc thời gian)

| Bước | Công việc | Công cụ |
|---|---|---|
| 0 | Diễn giải lại đề bài test cho dễ hiểu | Claude |
| 1 | Dựng khung ASP.NET Core Web API, thêm Swagger UI, health check | Claude |
| 2 | Thiết kế entity (User, Conversation, Message, RequestLog), EF Core + SQL Server, migration đầu tiên | Claude |
| 3 | JWT Authentication (register/login), băm mật khẩu bằng BCrypt | Claude |
| 4 | Tích hợp Gemini API lần đầu (`POST /ai/chat`) | ChatGPT |
| 5 | Nối DB thật vào luồng chat (lưu Conversation/Message, ghi RequestLog), retry + timeout, rate limiting, `GET /conversations`, `GET /usage`, exception middleware, structured output cho `/ai/analyze` | Claude |
| — | Sửa lỗi AI chưa nhớ hội thoại (nạp lại lịch sử tin nhắn trước khi gọi Gemini) | Claude |
| 6 | Docker hóa, cấu hình đa provider DB (SQL Server local / SQLite deploy), deploy lên Render | Claude |
| — | README, sơ đồ kiến trúc (Mermaid), database schema, Postman collection, AI_WORKLOG | Claude |

## 3. Prompt tiêu biểu

**Prompt 1**
> "Tôi hiện đang học môn học kì doanh nghiệp, môn này yêu cầu tôi phải đi thực tập, doanh nghiệp mà hiện tại tôi đang đăng ký thực tập trước khi tôi có thể bắt đầu thực tập thì có cho tôi một bài test, dưới đây là nội dung của bài test đó, bạn đọc giúp tôi và diễn đạt lại một cách dễ hiểu và cho tiết nhất nội dung của bài test nhé"

Kết quả: Claude giải thích chi tiết về bài test gồm có tổng quan bài test, đề bài, yêu cầu tối thiểu, phần cộng điểm, những thứ phải nộp và cách chấm điểm

**Prompt 2**
> "Tôi sẽ sử dụng asp.net core web API cho dự án này, bạn hãy đọc file rar của dự án mà tôi mới tạo và giúp tôi tạo một lộ trình hoặc kế hoạch cho dự án của tôi"

Kết quả: Claude lập ra một lộ trình, kế hoạch xây dụng dự án cho đến ngày cuối

**Prompt 3**
> "Bây giờ bạn với vai trò là một coder cùng tôi hoàn thành dự án này, hãy bắt đầu bước đầu tiên của dự án"

Kết quả: Claude bắt đầu với bước 1: Dựng khung web API và chạy được Swagger, bắt đầu bằng việc cài Swagger UI bằng lệnh `dotnet add package Swashbuckle.AspNetCore` , tạo cấu trúc thư mục, thay đổi code cho Program.cs, tạo HealthController.cs
--> Vấn đề: khi khởi động app, xuất hiện lỗi `ReflectionTypeLoadException`
--> Nguyên nhân: do tồn tại `Microsoft.AspNetCore.OpenApi` + `Swashbuckle.AspNetCore` cùng trong một dự án
--> Khắc phục: Gỡ package `Microsoft.AspNetCore.OpenApi` khỏi `.csproj`, chỉ giữ Swashbuckle

**Prompt 4**
> "Tôi đã gỡ package `Microsoft.AspNetCore.OpenApi` và dự án đã chạy tốt, tiếp theo hãy bắt đầu bước 2"

Kết quả: Claude bắt đầu với bước 2: Database, yêu cầu cài các package và công cụ cần thiết bằng lệnh `dotnet add package Microsoft.EntityFrameworkCore.SqlServer`, `dotnet add package Microsoft.EntityFrameworkCore.Design`, `dotnet tool install --global dotnet-ef`, tạo 4 entity cho Models, AppDbContext.cs, thiết lập kết nối với SQL Server, tạo database và migration bằng lệnh `dotnet ef migrations add InitialCreate`, `dotnet ef database update`
--> Vấn đề: khi khởi động app, xuất hiện lỗi `FileNotFoundException: Microsoft.EntityFrameworkCore, Version=9.0.20.0`
--> Nguyên nhân: do phiên bản hiện đang dùng là .Net 9 nhưng khi cài bằng lệnh sẽ tự động cài package phiên bản mới nhất (10.0.12), phiên bản này không tương thích với .Net 9 nên gây lỗi
--> Khắc phục: vào Manage Nuget Package để gỡ các package hiện tại và cài lại chúng với phiên bản 9.0.20

**Prompt 5**
> "Tôi đã kết nối thành công với SQL Server và database cũng đã được tạo thành công, tiếp theo chúng ta tiếp tục với bước 3"

Kết quả: Claude bắt đầu với bước 3: Authorization, yêu cầu cài package `Microsoft.AspNetCore.Authentication.JwtBearer` và `BCrypt.Net-Next`, cấu hình JWT vào appsetting.json, tạo AuthDtos.cs, AuthService.cs và AuthController.cs, Đăng ký JWT vào Program.cs, build và test

**Prompt 6**
> Gửi các file trong dự án mà ChatGPT cần "trước khi hỏi bạn tôi có thực hiện dự án cùng AI tên Claude, và AI đó đã hướng dẫn tôi thực hiện dựng khung web API, Swagger UI, tạo Database và Authorization bằng JWT, sau đây bạn hãy giúp tôi thực hiện bước tích hợp LLM vào dự án"

Kết quả: ChatGPT đề xuất Gemini AI, hướng dẫn lấy API Key miễn phí của Gemini, hướng dẫn tạo biến môi trường bằng lệnh `[Environment]::SetEnvironmentVariable("GEMINI_API_KEY", "API_KEY_CUA_HUY", "User")`, tạo AiService.cs, AiController.cs, build và test

**Prompt 7**
> Gửi lại dự án cho Claude "Tôi đã nhờ một AI khác là ChatGPT để làm bước 4 (Tích hợp LLM) và đã hoàn thành, trong swagger tôi đã hỏi phần Post ai/chat và Gemini cũng đã phản hồi lại, sau đây tôi sẽ gửi bạn file rar của dự án sau khi nhờ ChatGPT hướng dẫn tôi làm bước 4, bạn xem và tiếp tục hướng dẫn tôi làm các bước tiếp theo với tư cách là một coder sẽ cùng tôi hoàn thành dự án này"

Kết quả: Claude báo không có model `gemini-3.6-flash` và đề xuất các phiên bản khác `gemini-2.5-flash`, `gemini-2.5-pro`, bắt đầu với bước 5: Nối DB, RequestLog, Retry/Timeout, Rate limiting, Usage & Conversations, Claude sửa appsetting.json, tạo AiDtos.cs, ConservationsController.cs, UsageController.cs, ExceptionHandlingMiddleware.cs, nâng cấp code cho AiService.cs, AiController.cs, Program.cs, test các chức năng trên Swagger UI
--> Vấn đề: Claude báo không có model `gemini-3.6-flash` trong khi tôi đã sử dụng được ở lần test Post ai/chat cùng ChatGPT, trong khi các đề xuất model của Claude thì đã ngừng cấp, nên khi test Post ai/chat sẽ phát sinh lỗi
--> Khắc phục: giữ lại phiên bản model hiện tại

**Prompt 8**
>"Tôi test Post ai/chat, trước đó thì chạy được nhưng giờ lại không chạy được nữa, Error code 404, Response liên tục trả kết quả "message": "Không thể gọi Gemini.", "error": "Gemini API lỗi: TooManyRequests", trường hợp này có phải là do số lượng request của tôi đã chạm giới hạn hay không? Bạn có đề xuất phiên bản model nào khác của Gemini không"

Kết quả: Claude xác nhận là model `gemini-3.6-flash` chỉ có giới hạn là 20 request/ngày nên không thể để test nhiều lần trong Swagger, đề xuất `gemini-2.5-flash-lite` có quota rộng hơn và các phiên bản khác nữa nếu test vẫn bị lỗi `gemini-2.5-flash-lite`, `gemini-3.1-flash-lite-preview`, `gemini-3-flash-preview`
--> Vấn đề: Trong lúc test thì response trả về phiên bản `gemini-2.5-flash-lite` đã ngừng được cấp
--> Khắc phục: tự tìm hiểu phiên bản khác trên trang của Google và thay bằng model `gemini-3.5-flash-lite`

**Prompt 8**
>"Tôi đã kiểm tra hết các chức năng trên Swagger UI rồi và tất cả đều hoạt động tốt, tiếp theo bạn hãy cùng tôi thục hiện bước cuối cùng của dự án"

Kết quả: Claude bắt đàu với bước 6: Docker + Deploy, yêu cầu cài package `Microsoft.EntityFrameworkCore.Sqlite`, thay đổi code cho Program.cs, tạo dockerfile, .dockerignore, .gitignore, hướng dẫn deploy dự án lên Render.com(miễn phí)

## 4. Nếu có thêm 7 ngày

- Triển khai các phần bonus: **fallback model** (tự chuyển model khác khi model chính lỗi/hết quota), **caching** (tránh gọi lại Gemini với câu hỏi giống nhau), **cost estimation** (ước tính chi phí theo token), **queue-based processing** cho tải cao.
- Thêm **observability** thực sự: structured logging (Serilog), dashboard theo dõi usage theo thời gian thực thay vì chỉ 1 endpoint tổng hợp.
- Đổi database production sang **PostgreSQL** có persistent storage thay vì SQLite tạm thời trên Render free tier.
- Thêm **refresh token** cho JWT thay vì chỉ access token 120 phút.
- Phân trang (pagination) cho `GET /conversations` khi số lượng lớn.
- Viết **automated tests** (unit test cho `AiService`, integration test cho các controller).
- Thiết lập **CI/CD** (GitHub Actions) tự động build/test/deploy khi push code.
- Cho phép `/usage` lọc theo từng user, theo khoảng thời gian (ngày/tuần/tháng).
