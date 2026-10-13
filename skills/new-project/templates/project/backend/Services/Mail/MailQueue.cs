using System.Threading.Channels;

namespace backend.Services.Mail;

/// <summary>
/// 待寄送郵件
/// </summary>
public record MailItem(string ToEmail, string ToName, string Subject, string HtmlBody, string Category);

/// <summary>
/// 郵件佇列（有界，避免被大量申請耗盡記憶體）
/// </summary>
public class MailQueue
{
    private readonly Channel<MailItem> _channel = Channel.CreateBounded<MailItem>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropWrite
    });

    public bool TryEnqueue(MailItem item) => _channel.Writer.TryWrite(item);

    public IAsyncEnumerable<MailItem> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}
