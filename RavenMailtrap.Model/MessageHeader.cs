using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Mail;
using System.Net.Mime;

namespace RavenMailtrap.Model
{
    public sealed class MessageHeader
    {
        /// <summary>
        ///     All headers which were not recognized and explicitly dealt with.<br />
        ///     This should mostly be custom headers, which are marked as X-[name].<br />
        ///     <br />
        ///     This list will be empty if all headers were recognized and parsed.
        /// </summary>
        /// <remarks>
        ///     If you as a user, feels that a header in this collection should
        ///     be parsed, feel free to notify the developers.
        /// </remarks>
        public Dictionary<string, string> UnknownHeaders { get; set; }

        /// <summary>
        ///     A human readable description of the body<br />
        ///     <br />
        ///     <see langword="null" /> if no Content-Description header was present in the message.
        /// </summary>
        public string ContentDescription { get; set; }

        /// <summary>
        ///     ID of the content part (like an attached image). Used with MultiPart messages.<br />
        ///     <br />
        ///     <see langword="null" /> if no Content-ID header field was present in the message.
        /// </summary>
        /// <see cref="MessageId">For an ID of the message</see>
        public string ContentId { get; set; }

        /// <summary>
        ///     Message keywords<br />
        ///     <br />
        ///     The list will be empty if no Keywords header was present in the message
        /// </summary>
        public List<string> Keywords { get; set; }

        /// <summary>
        ///     A List of emails to people who wishes to be notified when some event happens.<br />
        ///     These events could be email:
        ///     <list type="bullet">
        ///         <item>deletion</item>
        ///         <item>printing</item>
        ///         <item>received</item>
        ///         <item>...</item>
        ///     </list>
        ///     The list will be empty if no Disposition-Notification-To header was present in the message
        /// </summary>
        /// <remarks>
        ///     See <a href="http://tools.ietf.org/html/rfc3798">RFC 3798</a> for details
        /// </remarks>
        public List<string> DispositionNotificationTo { get; set; }

        /// <summary>
        ///     This is the Received headers. This tells the path that the email went.<br />
        ///     <br />
        ///     The list will be empty if no Received header was present in the message
        /// </summary>
        public List<string> Received { get; set; }

        /// <summary>
        ///     Importance of this email.<br />
        ///     <br />
        ///     The importance level is set to normal, if no Importance header field was mentioned or it contained
        ///     unknown information. This is the expected behavior according to the RFC.
        /// </summary>
        public MailPriority Importance { get; set; }

        /// <summary>
        ///     This header describes the Content encoding during transfer.<br />
        ///     <br />
        ///     If no Content-Transfer-Encoding header was present in the message, it is set
        ///     to the default of <see cref="Header.ContentTransferEncoding.SevenBit">SevenBit</see> in accordance to the RFC.
        /// </summary>
        /// <remarks>
        ///     See <a href="http://tools.ietf.org/html/rfc2045#section-6">RFC 2045 section 6</a> for details
        /// </remarks>
        public string ContentTransferEncoding { get; set; }

        /// <summary>
        ///     Carbon Copy. This specifies who got a copy of the message.<br />
        ///     <br />
        ///     The list will be empty if no Cc header was present in the message
        /// </summary>
        public List<string> Cc { get; set; }

        /// <summary>
        ///     Blind Carbon Copy. This specifies who got a copy of the message, but others
        ///     cannot see who these persons are.<br />
        ///     <br />
        ///     The list will be empty if no Received Bcc was present in the message
        /// </summary>
        public List<string> Bcc { get; set; }

        /// <summary>
        ///     Specifies who this mail was for<br />
        ///     <br />
        ///     The list will be empty if no To header was present in the message
        /// </summary>
        public List<string> To { get; set; }

        /// <summary>
        ///     Specifies who sent the email<br />
        ///     <br />
        ///     <see langword="null" /> if no From header field was present in the message
        /// </summary>
        public string From { get; set; }

        /// <summary>
        ///     Specifies who a reply to the message should be sent to<br />
        ///     <br />
        ///     <see langword="null" /> if no Reply-To header field was present in the message
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        ///     The message identifier(s) of the original message(s) to which the
        ///     current message is a reply.<br />
        ///     <br />
        ///     The list will be empty if no In-Reply-To header was present in the message
        /// </summary>
        public List<string> InReplyTo { get; set; }


        /// <summary>
        ///     The message identifier(s) of other message(s) to which the current
        ///     message is related to.<br />
        ///     <br />
        ///     The list will be empty if no References header was present in the message
        /// </summary>
        public List<string> References { get; set; }

        /// <summary>
        ///     This is the sender of the email address.<br />
        ///     <br />
        ///     <see langword="null" /> if no Sender header field was present in the message
        /// </summary>
        /// <remarks>
        ///     The RFC states that this field can be used if a secretary
        ///     is sending an email for someone she is working for.
        ///     The email here will then be the secretary's email, and
        ///     the Reply-To field would hold the address of the person she works for.<br />
        ///     RFC states that if the Sender is the same as the From field,
        ///     sender should not be included in the message.
        /// </remarks>
        public string Sender { get; set; }

        /// <summary>
        ///     The Content-Type header field.<br />
        ///     <br />
        ///     If not set, the ContentType is created by the default "text/plain; charset=us-ascii" which is
        ///     defined in <a href="http://tools.ietf.org/html/rfc2045#section-5.2">RFC 2045 section 5.2</a>.<br />
        ///     If set, the default is overridden.
        /// </summary>
        public ContentType ContentType { get; set; }

        /// <summary>
        ///     Used to describe if a <see cref="MessagePart" /> is to be displayed or to be though of as an attachment.<br />
        ///     Also contains information about filename if such was sent.<br />
        ///     <br />
        ///     <see langword="null" /> if no Content-Disposition header field was present in the message
        /// </summary>
        public ContentDisposition ContentDisposition { get; set; }

        /// <summary>
        ///     The Date when the email was sent.<br />
        ///     This is the raw value. <see cref="DateSent" /> for a parsed up <see cref="DateTime" /> value of this field.<br />
        ///     <br />
        ///     <see langword="DateTime.MinValue" /> if no Date header field was present in the message or if the date could not be parsed.
        /// </summary>
        /// <remarks>
        ///     See <a href="http://tools.ietf.org/html/rfc5322#section-3.6.1">RFC 5322 section 3.6.1</a> for more details
        /// </remarks>
        public string Date { get; set; }

        /// <summary>
        ///     The Date when the email was sent.<br />
        ///     This is the parsed equivalent of <see cref="Date" />.<br />
        ///     Notice that the <see cref="TimeZone" /> of the <see cref="DateTime" /> object is in UTC and has NOT been converted
        ///     to local <see cref="TimeZone" />.
        /// </summary>
        /// <remarks>
        ///     See <a href="http://tools.ietf.org/html/rfc5322#section-3.6.1">RFC 5322 section 3.6.1</a> for more details
        /// </remarks>
        public DateTime DateSent { get; set; }

        /// <summary>
        ///     An ID of the message that is SUPPOSED to be in every message according to the RFC.<br />
        ///     The ID is unique.<br />
        ///     <br />
        ///     <see langword="null" /> if no Message-ID header field was present in the message
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        ///     The Mime Version.<br />
        ///     This field will almost always show 1.0<br />
        ///     <br />
        ///     <see langword="null" /> if no Mime-Version header field was present in the message
        /// </summary>
        public string MimeVersion { get; set; }

        /// <summary>
        ///     A single <see cref="RfcMailAddress" /> with no username inside.<br />
        ///     This is a trace header field, that should be in all messages.<br />
        ///     Replies should be sent to this address.<br />
        ///     <br />
        ///     <see langword="null" /> if no Return-Path header field was present in the message
        /// </summary>
        public string ReturnPath { get; set; }

        /// <summary>
        ///     The subject line of the message in decoded, one line state.<br />
        ///     This should be in all messages.<br />
        ///     <br />
        ///     <see langword="null" /> if no Subject header field was present in the message
        /// </summary>
        public string Subject { get; set; }
    }
}