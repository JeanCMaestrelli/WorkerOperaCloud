using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Services;
using WorkerOperaCloud.Services.Interfaces;

namespace WorkerOperaCloud.Repository.Jobs
{
    public class NameJob : IServiceJobs
    {
        private readonly ILogger<BusinessdateJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public NameJob(ILogger<BusinessdateJob> logger, DapperContext context, Job_Exec job_Exec)
        {
            _logger = logger;
            _context = context;
            _Job_Exec = job_Exec;
        }

        public void IntegrarJobAsync(string filePath, string id_job, string TIPO_AGENDAMENTO)
        {
            LoggBuilder.Clear();
            _stopwatch.Reset();
            _stopwatch.Start();
            DataExecucao = DateTime.Now.ToString();
            erros = "0";
            string IDEXEC = "";
            string queryInsert = "";
            string queryUpdate = "";
            long registros = 0;
            long regIntegrados = 0;
            long regAtualizados = 0;
            _TIPO_AGENDAMENTO = TIPO_AGENDAMENTO;
            OracleCommand command = new();

            LoggBuilder.AppendLine($"\n   ################## {DateTime.Now} INICIANDO INTEGRAÇÃO {TIPO_AGENDAMENTO} ##################\n");

            var List = _Job_Exec.Get_List<Mdl_Name>(filePath, LoggBuilder);

            registros = List.Count;
            if (List.Count > 0)
            {
                try
                {
                    Mdl_Job_Exec jobex = new()
                    {
                        ID_JOB = id_job,
                        JOB = TIPO_AGENDAMENTO,
                        RESORT = "",
                        INICIO = DataExecucao
                    };

                    IDEXEC = _Job_Exec.InsertJobExec(jobex);

                    LoggBuilder.AppendLine($"      {DateTime.Now} Criando conexão com banco de dados...");

                    command.Connection = _context.CreateConnection();

                    LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Abrindo conexão...");
                    command.Connection.Open();

                    LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Integrando...\n");
                    int x = 0;
                    foreach (var trial in List)
                    {
                        x++;
                        try
                        {
                            var _where = $"NAME_ID = '{trial.NAME_ID}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_Name>(query);

                            if (result.ToList().Count > 0)
                            {
                                if (result.ToList().Count > 1)
                                {
                                    erros = "1";
                                    LoggBuilder.AppendLine($"  ******  {DateTime.Now}  Registro duplicado, QTD: {result.ToList().Count}. \n{trial.Serialize()} ****** ");
                                }
                                else
                                {
                                    var bdUpdate = Convert.ToDateTime(result.FirstOrDefault().RNA_UPDATEDATE);//data vinda do banco
                                    var xmlUpdate = Convert.ToDateTime(trial.RNA_UPDATEDATE);//data vinda do xml
                                    var update = DateTime.Compare(xmlUpdate, bdUpdate);

                                    if (update == 1 || trial.DELETED_FLAG == "Y")//se data xml maior entao update
                                    {
                                        if (regAtualizados == 0)
                                            queryUpdate = _Job_Exec.GetQuery(TIPO_AGENDAMENTO, "U", LoggBuilder);

                                        try
                                        {
                                            command.CommandText = (queryUpdate + _where);
                                            MountParametros(command, trial);
                                            command.ExecuteNonQuery();
                                            regAtualizados++;
                                        }
                                        catch (Exception ex)
                                        {
                                            erros = "1";
                                            LoggBuilder.AppendLine($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                            _logger.LogInformation($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                        }

                                        command.Parameters.Clear();
                                    }
                                }
                            }
                            else //insert registros
                            {
                                if (regIntegrados == 0)
                                    queryInsert = _Job_Exec.GetQuery(TIPO_AGENDAMENTO, "I", LoggBuilder);

                                try
                                {
                                    command.CommandText = queryInsert;
                                    MountParametros(command, trial);
                                    command.ExecuteNonQuery();
                                    regIntegrados++;
                                }
                                catch (Exception ex)
                                {
                                    erros = "1";
                                    LoggBuilder.AppendLine($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                    _logger.LogInformation($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                }

                                command.Parameters.Clear();
                            }
                        }
                        catch (Exception e)
                        {
                            erros = "1";
                            LoggBuilder.AppendLine($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {e.Message}.\n{trial.Serialize()}");
                            _logger.LogInformation($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {e.Message}.\n{trial.Serialize()}");
                        }
                    }

                    command.Connection.Close();
                }
                catch (Exception e)
                {
                    erros = "1";
                    command.Parameters.Clear();
                    if (command.Connection.State == System.Data.ConnectionState.Open)
                        command.Connection.Close();
                    LoggBuilder.AppendLine($"\n      {DateTime.Now} Erro Exception: {e.Message}");
                }
            }
            else
            {
                if (erros == "0") { LoggBuilder.AppendLine($"\n      {DateTime.Now} Arquivo vazio, Sem registros para integrar."); }
            }
            LoggBuilder.AppendLine($"      {DateTime.Now} Integração Finalizada...");
            LoggBuilder.AppendLine($"\n      {DateTime.Now} Fechando conexão...");
            LoggBuilder.AppendLine($"\n      {DateTime.Now} Total de registros no arquivo {filePath.Split("\\")[filePath.Split("\\").Length - 1]}: {registros} registros.");
            LoggBuilder.AppendLine($"      {DateTime.Now} Total de registros integrados: {regIntegrados} registros.");
            LoggBuilder.AppendLine($"      {DateTime.Now} Total de registros atualizados: {regAtualizados} registros.");

            _stopwatch.Stop();
            LoggBuilder.AppendLine($"\n      {DateTime.Now} Tempo de integração: {_stopwatch.Elapsed.ToString().Split(".")[0]}.");

            LoggBuilder.AppendLine($"\n   ################## {DateTime.Now} FIM INTEGRAÇÂO {TIPO_AGENDAMENTO} ##################");
            _logger.LogInformation($"\n   ################## {DateTime.Now} FIM INTEGRAÇÂO {TIPO_AGENDAMENTO} ##################");

            _Job_Exec.InsertLog(id_job, TIPO_AGENDAMENTO, erros, _stopwatch, LoggBuilder, DataExecucao);

            _Job_Exec.DeletJobExec(IDEXEC);

        }

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Name nameJob)
        {
            parametros.Parameters.Add("NAME_ID", nameJob.NAME_ID);
            parametros.Parameters.Add("NAME_CODE", nameJob.NAME_CODE);
            parametros.Parameters.Add("INSERT_USER", nameJob.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", nameJob.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", nameJob.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", nameJob.UPDATE_DATE.ToString());
            parametros.Parameters.Add("PRIMARY_NAME_ID", nameJob.PRIMARY_NAME_ID);
            parametros.Parameters.Add("REPEAT_GUEST_ID", nameJob.REPEAT_GUEST_ID);
            parametros.Parameters.Add("MAIL_LIST", nameJob.MAIL_LIST);
            parametros.Parameters.Add("MAIL_TYPE", nameJob.MAIL_TYPE);
            parametros.Parameters.Add("FOLLOW_ON", nameJob.FOLLOW_ON);
            parametros.Parameters.Add("BUSINESS_TITLE", nameJob.BUSINESS_TITLE);
            parametros.Parameters.Add("INACTIVE_DATE", nameJob.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("ARC_UPDATE_DATE", nameJob.ARC_UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_FAX_DATE", nameJob.UPDATE_FAX_DATE.ToString());
            parametros.Parameters.Add("BIRTH_DATE", nameJob.BIRTH_DATE.ToString());
            parametros.Parameters.Add("COLLECTION_USER_ID", nameJob.COLLECTION_USER_ID);
            parametros.Parameters.Add("COMPANY", nameJob.COMPANY);
            parametros.Parameters.Add("SOUND_EX_COMPANY", nameJob.SOUND_EX_COMPANY);
            parametros.Parameters.Add("LEGAL_COMPANY", nameJob.LEGAL_COMPANY);
            parametros.Parameters.Add("FIRST", nameJob.FIRST);
            parametros.Parameters.Add("MIDDLE", nameJob.MIDDLE);
            parametros.Parameters.Add("LAST", nameJob.LAST);
            parametros.Parameters.Add("NICKNAME", nameJob.NICKNAME);
            parametros.Parameters.Add("TITLE", nameJob.TITLE);
            parametros.Parameters.Add("SOUND_EX_LAST", nameJob.SOUND_EX_LAST);
            parametros.Parameters.Add("EXTERNAL_REFERENCE_REQU", nameJob.EXTERNAL_REFERENCE_REQU);
            parametros.Parameters.Add("VIP_STATUS", nameJob.VIP_STATUS);
            parametros.Parameters.Add("VIP_AUTHORIZATION", nameJob.VIP_AUTHORIZATION);
            parametros.Parameters.Add("BILLING_PROFILE_CODE", nameJob.BILLING_PROFILE_CODE);
            parametros.Parameters.Add("RATE_STRUCTURE", nameJob.RATE_STRUCTURE);
            parametros.Parameters.Add("NAME_COMMENT", nameJob.NAME_COMMENT);
            parametros.Parameters.Add("TOUR_OPERATOR_TYPE", nameJob.TOUR_OPERATOR_TYPE);
            parametros.Parameters.Add("REGION", nameJob.REGION);
            parametros.Parameters.Add("TYPE_OF_1099", nameJob.TYPE_OF_1099);
            parametros.Parameters.Add("TAX1_NO", nameJob.TAX1_NO);
            parametros.Parameters.Add("COMPANY_NAME_ID", nameJob.COMPANY_NAME_ID);
            parametros.Parameters.Add("EXTERNAL_REFERENCE_REQUIRED", nameJob.EXTERNAL_REFERENCE_REQUIRED);
            parametros.Parameters.Add("VENDOR_ID", nameJob.VENDOR_ID);
            parametros.Parameters.Add("VENDOR_SITE_ID", nameJob.VENDOR_SITE_ID);
            parametros.Parameters.Add("ARC_OFFICE_TYPE", nameJob.ARC_OFFICE_TYPE);
            parametros.Parameters.Add("TAX2_NO", nameJob.TAX2_NO);
            parametros.Parameters.Add("ARC_MAIL_FLAG", nameJob.ARC_MAIL_FLAG);
            parametros.Parameters.Add("NAME2", nameJob.NAME2);
            parametros.Parameters.Add("NAME3", nameJob.NAME3);
            parametros.Parameters.Add("SALESREP", nameJob.SALESREP);
            parametros.Parameters.Add("TRACECODE", nameJob.TRACECODE);
            parametros.Parameters.Add("GEOGRAPHIC_REGION", nameJob.GEOGRAPHIC_REGION);
            parametros.Parameters.Add("GUEST_CLASSIFICATION", nameJob.GUEST_CLASSIFICATION);
            parametros.Parameters.Add("PRIMARY_ADDRESS_ID", nameJob.PRIMARY_ADDRESS_ID);
            parametros.Parameters.Add("PRIMARY_PHONE_ID", nameJob.PRIMARY_PHONE_ID);
            parametros.Parameters.Add("TAX_EXEMPT_STATUS", nameJob.TAX_EXEMPT_STATUS);
            parametros.Parameters.Add("GDS_NAME", nameJob.GDS_NAME);
            parametros.Parameters.Add("GDS_TRANSACTION_NO", nameJob.GDS_TRANSACTION_NO);
            parametros.Parameters.Add("NATIONALITY", nameJob.NATIONALITY);
            parametros.Parameters.Add("LANGUAGE", nameJob.LANGUAGE);
            parametros.Parameters.Add("SALUTATION", nameJob.SALUTATION);
            parametros.Parameters.Add("PASSPORT", nameJob.PASSPORT);
            parametros.Parameters.Add("HISTORY_YN", nameJob.HISTORY_YN);
            parametros.Parameters.Add("RESV_CONTACT", nameJob.RESV_CONTACT);
            parametros.Parameters.Add("CONTRACT_NO", nameJob.CONTRACT_NO);
            parametros.Parameters.Add("CONTRACT_RECV_DATE", nameJob.CONTRACT_RECV_DATE.ToString());
            parametros.Parameters.Add("ACCT_CONTACT", nameJob.ACCT_CONTACT);
            parametros.Parameters.Add("PRIORITY", nameJob.PRIORITY);
            parametros.Parameters.Add("INDUSTRY_CODE", nameJob.INDUSTRY_CODE);
            parametros.Parameters.Add("ROOMS_POTENTIAL", nameJob.ROOMS_POTENTIAL);
            parametros.Parameters.Add("COMPETITION_CODE", nameJob.COMPETITION_CODE);
            parametros.Parameters.Add("SCOPE", nameJob.SCOPE);
            parametros.Parameters.Add("SCOPE_CITY", nameJob.SCOPE_CITY);
            parametros.Parameters.Add("TERRITORY", nameJob.TERRITORY);
            parametros.Parameters.Add("ACTIONCODE", nameJob.ACTIONCODE);
            parametros.Parameters.Add("ACTIVE_YN", nameJob.ACTIVE_YN);
            parametros.Parameters.Add("MASTER_ACCOUNT_YN", nameJob.MASTER_ACCOUNT_YN);
            parametros.Parameters.Add("NAME_TYPE", nameJob.NAME_TYPE);
            parametros.Parameters.Add("SNAME", nameJob.SNAME);
            parametros.Parameters.Add("NAME_TAX_TYPE", nameJob.NAME_TAX_TYPE);
            parametros.Parameters.Add("SFIRST", nameJob.SFIRST);
            parametros.Parameters.Add("AR_NO", nameJob.AR_NO);
            parametros.Parameters.Add("AVAILABILITY_OVERRIDE", nameJob.AVAILABILITY_OVERRIDE);
            parametros.Parameters.Add("BILLING_CODE", nameJob.BILLING_CODE);
            parametros.Parameters.Add("CASH_BL_IND", nameJob.CASH_BL_IND);
            parametros.Parameters.Add("BL_MSG", nameJob.BL_MSG);
            parametros.Parameters.Add("CURRENCY_CODE", nameJob.CURRENCY_CODE);
            parametros.Parameters.Add("COMMISSION_CODE", nameJob.COMMISSION_CODE);
            parametros.Parameters.Add("HOLD_CODE", nameJob.HOLD_CODE);
            parametros.Parameters.Add("INTEREST", nameJob.INTEREST);
            parametros.Parameters.Add("SUMM_REF_CC", nameJob.SUMM_REF_CC);
            parametros.Parameters.Add("IATA_COMP_TYPE", nameJob.IATA_COMP_TYPE);
            parametros.Parameters.Add("SREP_CODE", nameJob.SREP_CODE);
            parametros.Parameters.Add("ACCOUNTSOURCE", nameJob.ACCOUNTSOURCE);
            parametros.Parameters.Add("MARKETS", nameJob.MARKETS);
            parametros.Parameters.Add("PRODUCT_INTEREST", nameJob.PRODUCT_INTEREST);
            parametros.Parameters.Add("KEYWORD", nameJob.KEYWORD);
            parametros.Parameters.Add("LETTER_GREETING", nameJob.LETTER_GREETING);
            parametros.Parameters.Add("INFLUENCE", nameJob.INFLUENCE);
            parametros.Parameters.Add("DEPT_ID", nameJob.DEPT_ID);
            parametros.Parameters.Add("DEPARTMENT", nameJob.DEPARTMENT);
            parametros.Parameters.Add("CONTACT_YN", nameJob.CONTACT_YN);
            parametros.Parameters.Add("ACCOUNT_TYPE", nameJob.ACCOUNT_TYPE);
            parametros.Parameters.Add("DOWNLOAD_RESORT", nameJob.DOWNLOAD_RESORT);
            parametros.Parameters.Add("DOWNLOAD_SREP", nameJob.DOWNLOAD_SREP);
            parametros.Parameters.Add("DOWNLOAD_DATE", nameJob.DOWNLOAD_DATE.ToString());
            parametros.Parameters.Add("UPLOAD_DATE", nameJob.UPLOAD_DATE.ToString());
            parametros.Parameters.Add("LAPTOP_CHANGE", nameJob.LAPTOP_CHANGE);
            parametros.Parameters.Add("CRS_NAMEID", nameJob.CRS_NAMEID);
            parametros.Parameters.Add("COMM_PAY_CENTRAL", nameJob.COMM_PAY_CENTRAL);
            parametros.Parameters.Add("CC_PROFILE_YN", nameJob.CC_PROFILE_YN);
            parametros.Parameters.Add("GENDER", nameJob.GENDER);
            parametros.Parameters.Add("BIRTH_PLACE", nameJob.BIRTH_PLACE);
            parametros.Parameters.Add("BIRTH_COUNTRY", nameJob.BIRTH_COUNTRY);
            parametros.Parameters.Add("PROFESSION", nameJob.PROFESSION);
            parametros.Parameters.Add("ID_TYPE", nameJob.ID_TYPE);
            parametros.Parameters.Add("ID_NUMBER", nameJob.ID_NUMBER);
            parametros.Parameters.Add("ID_DATE", nameJob.ID_DATE.ToString());
            parametros.Parameters.Add("ID_PLACE", nameJob.ID_PLACE);
            parametros.Parameters.Add("ID_COUNTRY", nameJob.ID_COUNTRY);
            parametros.Parameters.Add("UDFC01", nameJob.UDFC01);
            parametros.Parameters.Add("UDFC02", nameJob.UDFC02);
            parametros.Parameters.Add("UDFC03", nameJob.UDFC03);
            parametros.Parameters.Add("UDFC04", nameJob.UDFC04);
            parametros.Parameters.Add("UDFC05", nameJob.UDFC05);
            parametros.Parameters.Add("UDFC06", nameJob.UDFC06);
            parametros.Parameters.Add("UDFC07", nameJob.UDFC07);
            parametros.Parameters.Add("UDFC08", nameJob.UDFC08);
            parametros.Parameters.Add("UDFC09", nameJob.UDFC09);
            parametros.Parameters.Add("UDFC10", nameJob.UDFC10);
            parametros.Parameters.Add("UDFC11", nameJob.UDFC11);
            parametros.Parameters.Add("UDFC12", nameJob.UDFC12);
            parametros.Parameters.Add("UDFC13", nameJob.UDFC13);
            parametros.Parameters.Add("UDFC14", nameJob.UDFC14);
            parametros.Parameters.Add("UDFC15", nameJob.UDFC15);
            parametros.Parameters.Add("UDFC16", nameJob.UDFC16);
            parametros.Parameters.Add("UDFC17", nameJob.UDFC17);
            parametros.Parameters.Add("UDFC18", nameJob.UDFC18);
            parametros.Parameters.Add("UDFC19", nameJob.UDFC19);
            parametros.Parameters.Add("UDFC20", nameJob.UDFC20);
            parametros.Parameters.Add("UDFC21", nameJob.UDFC21);
            parametros.Parameters.Add("UDFC22", nameJob.UDFC22);
            parametros.Parameters.Add("UDFC23", nameJob.UDFC23);
            parametros.Parameters.Add("UDFC24", nameJob.UDFC24);
            parametros.Parameters.Add("UDFC25", nameJob.UDFC25);
            parametros.Parameters.Add("UDFC26", nameJob.UDFC26);
            parametros.Parameters.Add("UDFC27", nameJob.UDFC27);
            parametros.Parameters.Add("UDFC28", nameJob.UDFC28);
            parametros.Parameters.Add("UDFC29", nameJob.UDFC29);
            parametros.Parameters.Add("UDFC30", nameJob.UDFC30);
            parametros.Parameters.Add("UDFC31", nameJob.UDFC31);
            parametros.Parameters.Add("UDFC32", nameJob.UDFC32);
            parametros.Parameters.Add("UDFC33", nameJob.UDFC33);
            parametros.Parameters.Add("UDFC34", nameJob.UDFC34);
            parametros.Parameters.Add("UDFC35", nameJob.UDFC35);
            parametros.Parameters.Add("UDFC36", nameJob.UDFC36);
            parametros.Parameters.Add("UDFC37", nameJob.UDFC37);
            parametros.Parameters.Add("UDFC38", nameJob.UDFC38);
            parametros.Parameters.Add("UDFC39", nameJob.UDFC39);
            parametros.Parameters.Add("UDFC40", nameJob.UDFC40);
            parametros.Parameters.Add("UDFN01", nameJob.UDFN01);
            parametros.Parameters.Add("UDFN02", nameJob.UDFN02);
            parametros.Parameters.Add("UDFN03", nameJob.UDFN03);
            parametros.Parameters.Add("UDFN04", nameJob.UDFN04);
            parametros.Parameters.Add("UDFN05", nameJob.UDFN05);
            parametros.Parameters.Add("UDFN06", nameJob.UDFN06);
            parametros.Parameters.Add("UDFN07", nameJob.UDFN07);
            parametros.Parameters.Add("UDFN08", nameJob.UDFN08);
            parametros.Parameters.Add("UDFN09", nameJob.UDFN09);
            parametros.Parameters.Add("UDFN10", nameJob.UDFN10);
            parametros.Parameters.Add("UDFN11", nameJob.UDFN11);
            parametros.Parameters.Add("UDFN12", nameJob.UDFN12);
            parametros.Parameters.Add("UDFN13", nameJob.UDFN13);
            parametros.Parameters.Add("UDFN14", nameJob.UDFN14);
            parametros.Parameters.Add("UDFN15", nameJob.UDFN15);
            parametros.Parameters.Add("UDFN16", nameJob.UDFN16);
            parametros.Parameters.Add("UDFN17", nameJob.UDFN17);
            parametros.Parameters.Add("UDFN18", nameJob.UDFN18);
            parametros.Parameters.Add("UDFN19", nameJob.UDFN19);
            parametros.Parameters.Add("UDFN20", nameJob.UDFN20);
            parametros.Parameters.Add("UDFN21", nameJob.UDFN21);
            parametros.Parameters.Add("UDFN22", nameJob.UDFN22);
            parametros.Parameters.Add("UDFN23", nameJob.UDFN23);
            parametros.Parameters.Add("UDFN24", nameJob.UDFN24);
            parametros.Parameters.Add("UDFN25", nameJob.UDFN25);
            parametros.Parameters.Add("UDFN26", nameJob.UDFN26);
            parametros.Parameters.Add("UDFN27", nameJob.UDFN27);
            parametros.Parameters.Add("UDFN28", nameJob.UDFN28);
            parametros.Parameters.Add("UDFN29", nameJob.UDFN29);
            parametros.Parameters.Add("UDFN30", nameJob.UDFN30);
            parametros.Parameters.Add("UDFN31", nameJob.UDFN31);
            parametros.Parameters.Add("UDFN32", nameJob.UDFN32);
            parametros.Parameters.Add("UDFN33", nameJob.UDFN33);
            parametros.Parameters.Add("UDFN34", nameJob.UDFN34);
            parametros.Parameters.Add("UDFN35", nameJob.UDFN35);
            parametros.Parameters.Add("UDFN36", nameJob.UDFN36);
            parametros.Parameters.Add("UDFN37", nameJob.UDFN37);
            parametros.Parameters.Add("UDFN38", nameJob.UDFN38);
            parametros.Parameters.Add("UDFN39", nameJob.UDFN39);
            parametros.Parameters.Add("UDFN40", nameJob.UDFN40);
            parametros.Parameters.Add("UDFD01", nameJob.UDFD01.ToString());
            parametros.Parameters.Add("UDFD02", nameJob.UDFD02.ToString());
            parametros.Parameters.Add("UDFD03", nameJob.UDFD03.ToString());
            parametros.Parameters.Add("UDFD04", nameJob.UDFD04.ToString());
            parametros.Parameters.Add("UDFD05", nameJob.UDFD05.ToString());
            parametros.Parameters.Add("UDFD06", nameJob.UDFD06.ToString());
            parametros.Parameters.Add("UDFD07", nameJob.UDFD07.ToString());
            parametros.Parameters.Add("UDFD08", nameJob.UDFD08.ToString());
            parametros.Parameters.Add("UDFD09", nameJob.UDFD09.ToString());
            parametros.Parameters.Add("UDFD10", nameJob.UDFD10.ToString());
            parametros.Parameters.Add("UDFD11", nameJob.UDFD11.ToString());
            parametros.Parameters.Add("UDFD12", nameJob.UDFD12.ToString());
            parametros.Parameters.Add("UDFD13", nameJob.UDFD13.ToString());
            parametros.Parameters.Add("UDFD14", nameJob.UDFD14.ToString());
            parametros.Parameters.Add("UDFD15", nameJob.UDFD15.ToString());
            parametros.Parameters.Add("UDFD16", nameJob.UDFD16.ToString());
            parametros.Parameters.Add("UDFD17", nameJob.UDFD17.ToString());
            parametros.Parameters.Add("UDFD18", nameJob.UDFD18.ToString());
            parametros.Parameters.Add("UDFD19", nameJob.UDFD19.ToString());
            parametros.Parameters.Add("UDFD20", nameJob.UDFD20.ToString());
            parametros.Parameters.Add("PAYMENT_DUE_DAYS", nameJob.PAYMENT_DUE_DAYS);
            parametros.Parameters.Add("SUFFIX", nameJob.SUFFIX);
            parametros.Parameters.Add("EXTERNAL_ID", nameJob.EXTERNAL_ID);
            parametros.Parameters.Add("GUEST_PRIV_YN", nameJob.GUEST_PRIV_YN);
            parametros.Parameters.Add("EMAIL_YN", nameJob.EMAIL_YN);
            parametros.Parameters.Add("MAIL_YN", nameJob.MAIL_YN);
            parametros.Parameters.Add("INDEX_NAME", nameJob.INDEX_NAME);
            parametros.Parameters.Add("XLAST_NAME", nameJob.XLAST_NAME);
            parametros.Parameters.Add("XFIRST_NAME", nameJob.XFIRST_NAME);
            parametros.Parameters.Add("XCOMPANY_NAME", nameJob.XCOMPANY_NAME);
            parametros.Parameters.Add("XTITLE", nameJob.XTITLE);
            parametros.Parameters.Add("XSALUTATION", nameJob.XSALUTATION);
            parametros.Parameters.Add("SXNAME", nameJob.SXNAME);
            parametros.Parameters.Add("SXFIRST_NAME", nameJob.SXFIRST_NAME);
            parametros.Parameters.Add("LAST_UPDATED_RESORT", nameJob.LAST_UPDATED_RESORT);
            parametros.Parameters.Add("ENVELOPE_GREETING", nameJob.ENVELOPE_GREETING);
            parametros.Parameters.Add("XENVELOPE_GREETING", nameJob.XENVELOPE_GREETING);
            parametros.Parameters.Add("DIRECT_BILL_BATCH_TYPE", nameJob.DIRECT_BILL_BATCH_TYPE);
            parametros.Parameters.Add("RESORT_REGISTERED", nameJob.RESORT_REGISTERED);
            parametros.Parameters.Add("TAX_OFFICE", nameJob.TAX_OFFICE);
            parametros.Parameters.Add("TAX_TYPE", nameJob.TAX_TYPE);
            parametros.Parameters.Add("TAX_CATEGORY", nameJob.TAX_CATEGORY);
            parametros.Parameters.Add("PREFERRED_ROOM_NO", nameJob.PREFERRED_ROOM_NO);
            parametros.Parameters.Add("PHONE_YN", nameJob.PHONE_YN);
            parametros.Parameters.Add("SMS_YN", nameJob.SMS_YN);
            parametros.Parameters.Add("PROTECTED", nameJob.PROTECTED);
            parametros.Parameters.Add("XLANGUAGE", nameJob.XLANGUAGE);
            parametros.Parameters.Add("MARKET_RESEARCH_YN", nameJob.MARKET_RESEARCH_YN);
            parametros.Parameters.Add("THIRD_PARTY_YN", nameJob.THIRD_PARTY_YN);
            parametros.Parameters.Add("AUTOENROLL_MEMBER_YN", nameJob.AUTOENROLL_MEMBER_YN);
            parametros.Parameters.Add("CHAIN_CODE", nameJob.CHAIN_CODE);
            parametros.Parameters.Add("CREDIT_RATING", nameJob.CREDIT_RATING);
            parametros.Parameters.Add("TITLE_SUFFIX", nameJob.TITLE_SUFFIX);
            parametros.Parameters.Add("COMPANY_GROUP_ID", nameJob.COMPANY_GROUP_ID);
            parametros.Parameters.Add("INACTIVE_REASON", nameJob.INACTIVE_REASON);
            parametros.Parameters.Add("IATA_CONSORTIA", nameJob.IATA_CONSORTIA);
            parametros.Parameters.Add("INCLUDE_IN_1099_YN", nameJob.INCLUDE_IN_1099_YN);
            parametros.Parameters.Add("PSUEDO_PROFILE_YN", nameJob.PSUEDO_PROFILE_YN);
            parametros.Parameters.Add("PROFILE_PRIVACY_FLG", nameJob.PROFILE_PRIVACY_FLG);
            parametros.Parameters.Add("REPLACE_ADDRESS", nameJob.REPLACE_ADDRESS);
            parametros.Parameters.Add("ALIEN_REGISTRATION_NO", nameJob.ALIEN_REGISTRATION_NO);
            parametros.Parameters.Add("IMMIGRATION_STATUS", nameJob.IMMIGRATION_STATUS);
            parametros.Parameters.Add("VISA_VALIDITY_TYPE", nameJob.VISA_VALIDITY_TYPE);
            parametros.Parameters.Add("ID_DOCUMENT_ATTACH_ID", nameJob.ID_DOCUMENT_ATTACH_ID);
            parametros.Parameters.Add("SUPER_SEARCH_INDEX_TEXT", nameJob.SUPER_SEARCH_INDEX_TEXT);
            parametros.Parameters.Add("BIRTH_DATE_STR", nameJob.BIRTH_DATE_STR);
            parametros.Parameters.Add("ORIG_NAME_ID", nameJob.ORIG_NAME_ID);
            parametros.Parameters.Add("D_OPT_IN_MAIL_LIST_FLG", nameJob.D_OPT_IN_MAIL_LIST_FLG);
            parametros.Parameters.Add("D_OPT_IN_MARKET_RESEARCH_FLG", nameJob.D_OPT_IN_MARKET_RESEARCH_FLG);
            parametros.Parameters.Add("D_OPT_IN_THIRD_PARTY_FLG", nameJob.D_OPT_IN_THIRD_PARTY_FLG);
            parametros.Parameters.Add("D_OPT_IN_AUTOENROLL_MEMBER_FLG", nameJob.D_OPT_IN_AUTOENROLL_MEMBER_FLG);
            parametros.Parameters.Add("D_OPT_IN_EMAIL_FLG", nameJob.D_OPT_IN_EMAIL_FLG);
            parametros.Parameters.Add("D_OPT_IN_PHONE_FLG", nameJob.D_OPT_IN_PHONE_FLG);
            parametros.Parameters.Add("D_OPT_IN_SMS_FLG", nameJob.D_OPT_IN_SMS_FLG);
            parametros.Parameters.Add("D_OPT_IN_GUEST_PRIV_FLG", nameJob.D_OPT_IN_GUEST_PRIV_FLG);
            parametros.Parameters.Add("AR_CREDIT_LIMIT_YN", nameJob.AR_CREDIT_LIMIT_YN);
            parametros.Parameters.Add("PROFILE_CREDIT_LIMIT", nameJob.PROFILE_CREDIT_LIMIT);
            parametros.Parameters.Add("XMIDDLE_NAME", nameJob.XMIDDLE_NAME);
            parametros.Parameters.Add("E_INVOICE_LIABLE_YN", nameJob.E_INVOICE_LIABLE_YN);
            parametros.Parameters.Add("E_INV_LIABLE_LAST_UPDATED", nameJob.E_INV_LIABLE_LAST_UPDATED.ToString());
            parametros.Parameters.Add("INTERNAL_BILL_YN", nameJob.INTERNAL_BILL_YN);
            parametros.Parameters.Add("ANONYMIZATION_STATUS", nameJob.ANONYMIZATION_STATUS);
            parametros.Parameters.Add("ANONYMIZATION_DATE", nameJob.ANONYMIZATION_DATE.ToString());
            parametros.Parameters.Add("RNA_INSERTDATE", nameJob.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", nameJob.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", nameJob.DELETED_FLAG);

            return parametros;
        }
    }
}
