namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal static class Consts
    {
        internal const string BetSchema = "bet";
        internal const string ReportSchema = "report";
        internal const string LogSchema = "log";

        private const string Namespace = "TGS.Core.DataServices.Tgs.SqlScripts";

        internal static readonly string ProcedureSqlFormat = $"{Namespace}.Procedures.{{0}}.sql";
        internal static readonly string FunctionSqlFormat = $"{Namespace}.Functions.{{0}}.sql";
        internal static readonly string IndexSqlFormat = $"{Namespace}.Indexes.{{0}}.sql";
        internal static readonly string PaymentInitialSqlFormat = $"{Namespace}.PaymentInitial.{{0}}.sql";
        internal static readonly string ViewSqlFormat = $"{Namespace}.Views.{{0}}.sql";
    }
}