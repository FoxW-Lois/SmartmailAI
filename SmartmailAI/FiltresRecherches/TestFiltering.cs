using System;
using System.Collections.Generic;
using System.Linq;

namespace MailFiltering
{
    // =========================
    // MODELE MAIL
    // =========================
    public sealed class MailMessage
    {
        public Guid Id { get; init; }
        public string From { get; init; } = "";
        public string To { get; init; } = "";
        public string Subject { get; init; } = "";
        public string Body { get; init; } = "";
        public DateTime Date { get; init; }
        public bool IsRead { get; init; }
        public bool HasAttachments { get; init; }
        public string Folder { get; init; } = "";
        public long Size { get; init; }
    }

    // =========================
    // INTERFACE FILTRE
    // =========================
    public interface IMailFilter
    {
        bool Match(MailMessage mail);
    }

    // =========================
    // FILTRES SIMPLES
    // =========================
    public sealed class TextContainsFilter : IMailFilter
    {
        private readonly Func<MailMessage, string> _selector;
        private readonly string _value;

        public TextContainsFilter(Func<MailMessage, string> selector, string value)
        {
            _selector = selector;
            _value = value;
        }

        public bool Match(MailMessage mail)
            => _selector(mail)
                .Contains(_value, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class DateAfterFilter : IMailFilter
    {
        private readonly DateTime _date;
        public DateAfterFilter(DateTime date) => _date = date;
        public bool Match(MailMessage mail) => mail.Date >= _date;
    }

    public sealed class DateBeforeFilter : IMailFilter
    {
        private readonly DateTime _date;
        public DateBeforeFilter(DateTime date) => _date = date;
        public bool Match(MailMessage mail) => mail.Date <= _date;
    }

    public sealed class BooleanFilter : IMailFilter
    {
        private readonly Func<MailMessage, bool> _selector;
        private readonly bool _expected;

        public BooleanFilter(Func<MailMessage, bool> selector, bool expected)
        {
            _selector = selector;
            _expected = expected;
        }

        public bool Match(MailMessage mail)
            => _selector(mail) == _expected;
    }

    public sealed class SizeGreaterThanFilter : IMailFilter
    {
        private readonly long _size;
        public SizeGreaterThanFilter(long size) => _size = size;
        public bool Match(MailMessage mail) => mail.Size >= _size;
    }

    // =========================
    // FILTRES COMPOSES
    // =========================
    public enum LogicalOperator
    {
        And,
        Or
    }

    public sealed class FilterGroup : IMailFilter
    {
        public LogicalOperator Operator { get; }
        public IReadOnlyList<IMailFilter> Filters { get; }

        public FilterGroup(LogicalOperator op, params IMailFilter[] filters)
        {
            Operator = op;
            Filters = filters;
        }

        public bool Match(MailMessage mail)
        {
            return Operator switch
            {
                LogicalOperator.And => Filters.All(f => f.Match(mail)),
                LogicalOperator.Or  => Filters.Any(f => f.Match(mail)),
                _ => false
            };
        }
    }

    public sealed class NotFilter : IMailFilter
    {
        private readonly IMailFilter _filter;
        public NotFilter(IMailFilter filter) => _filter = filter;
        public bool Match(MailMessage mail) => !_filter.Match(mail);
    }

    // =========================
    // SERVICE DE RECHERCHE
    // =========================
    public static class MailSearch
    {
        public static IEnumerable<MailMessage> Apply(
            IEnumerable<MailMessage> mails,
            IMailFilter filter)
        {
            return mails.Where(filter.Match);
        }
    }

    // =========================
    // EXEMPLES D'USAGE
    // =========================
    /*
        var filter = new FilterGroup(
            LogicalOperator.And,
            new TextContainsFilter(m => m.From, "paul"),
            new BooleanFilter(m => m.HasAttachments, true),
            new DateAfterFilter(DateTime.Today.AddDays(-7))
        );

        var result = MailSearch.Apply(allMails, filter);
    */
}
