namespace BaselExportLambda
{
    public class QueriesAthena
    {
        public static string GetSelectTableAthena(TypeExportBasel type, string referenceDatePath)
        {
            string query;

            query = $"SELECT * FROM dbbasel.view_basel WHERE \"DATA DA POSIÇÃO\" = '{referenceDatePath}' ";

            if (type == TypeExportBasel.Swap)
            {
                query += "AND \"PRODUTO SIG\" LIKE 'Swap%'";
            }
            else if (type == TypeExportBasel.Credit)
            {
                query += "AND \"PRODUTO SIG\" LIKE 'Crédito%'";
            }
            else if (type == TypeExportBasel.Others)
            {
                query += "AND \"PRODUTO SIG\" NOT LIKE 'Swap%' AND \"PRODUTO SIG\" NOT LIKE 'Crédito%' ";
            }
            return query;
        }

        public static string GetSelectExportAthena(TypeExportBasel type, string referenceDatePath)
        {
            string query = $@"SELECT id_parquet AS ""0""
                                FROM ""dbbasel"".""tb_basel_principal"" WHERE data_referencia_pasta = '{referenceDatePath}' ";

            if (type == TypeExportBasel.Swap)
            {
                query += "AND \"PRODUTO_SIG\" LIKE 'Swap%' ";
            }
            else if (type == TypeExportBasel.Credit)
            {
                query += "AND \"PRODUTO_SIG\" LIKE 'Crédito%' ";
            }
            else if (type == TypeExportBasel.Others)
            {
                query += "AND \"PRODUTO_SIG\" NOT LIKE 'Swap%' AND \"PRODUTO_SIG\" NOT LIKE 'Crédito%' ";
            }

            query += $@" AND id_parquet not in (SELECT id_parquet FROM ""dbbasel"".""tb_basel_control_export"" WHERE data_referencia_pasta = '{referenceDatePath}' GROUP BY id_parquet)
                                GROUP BY id_parquet";

            return query;
        }
    }
}
