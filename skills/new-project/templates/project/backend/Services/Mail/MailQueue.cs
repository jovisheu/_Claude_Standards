using System.Threading.Channels;

namespace backend.Services.Mail;

/// <summary>待寄送的郵件；Category 為郵件類別（如「重設密碼」），供紀錄使用，不含收件人與內文</summary>
public record MailMessageItem(string ToEmail, string ToName, string Subject, string HtmlBody, string Category);

/// <summary>郵件佇列（Singleton）：郵件於背景寄送，避免以回應時間探測帳號</summary>
public class MailQueue
{
    private readonly Channel<MailMessageItem> _channel = Channel.CreateBounded<MailMessageItem>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
    });

    public bool TryEnqueue(MailMessageItem item) => _channel.Writer.TryWrite(item);

    public IAsyncEnumerable<MailMessageItem> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}
