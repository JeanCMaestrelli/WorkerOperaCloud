using WorkerOperaCloud;
using WorkerOperaCloud.Jobs;
using WorkerOperaCloud.Repository.Jobs;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Triggers.Interfaces;
using WorkerOperaCloud.Triggers.Respository;
using WorkerOperaCloud.Services.Interfaces;
using WorkerOperaCloud.Services.Repository;
using WorkerOperaCloud.Services;
using ConvertCsvToJson;

var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>()

        //#### SCHEDULER, VERIFICADOR DOS AGENDAMENTOS #####
        .AddSingleton<IServiceScheduler, ServiceScheduler>()

        //#### CONTROLE DE EXECUÇÃO DE JOBS #####
        .AddScoped<Job_Exec>()

        //#### JOBS #####
        .AddScoped<IServiceJobs, BusinessdateJob>()
        .AddScoped<IServiceJobs, Trial_BalanceJob>()
        .AddScoped<IServiceJobs, NameJob>()
        .AddScoped<IServiceJobs, Name_AddressJob>()
        .AddScoped<IServiceJobs, Name_PhoneJob>()
        .AddScoped<IServiceJobs, Rep_ManagerJob>()
        .AddScoped<IServiceJobs, Financial_TransactionsJob>()
        .AddScoped<IServiceJobs, Trxs_Codes_ArrangementJob>()
        .AddScoped<IServiceJobs, Trxs_CodesJob>()
        .AddScoped<IServiceJobs, Reservation_NameJob>()
        .AddScoped<IServiceJobs, Folio_TaxJob>()
        .AddScoped<IServiceJobs, Financial_Transactions_JrnlJob>()
        .AddScoped<IServiceJobs, EventReservationJob>()
        .AddScoped<IServiceJobs, GemEventJob>()
        .AddScoped<IServiceJobs, GemEventRevenueJob>()
        .AddScoped<IServiceJobs, ReservationSummaryJob>()
        .AddScoped<IServiceJobs, Reservation_Stat_DailyJob>()
        .AddScoped<IServiceJobs, Allotment_HeaderJob>()
        .AddScoped<IServiceJobs, Ars_AccountJob>()
        .AddScoped<IServiceJobs, NamesOwnerJob>()
        .AddScoped<IServiceJobs, Name_CommissionJob>()
        .AddScoped<IServiceJobs, Postal_Codes_ChainJob>()
        .AddScoped<IServiceJobs, Resorts_MarketsJob>()
        .AddScoped<IServiceJobs, Resorts_Room_CategoryJob>()
        .AddScoped<IServiceJobs, Resort_Rate_CategoryJob>()
        .AddScoped<IServiceJobs, Resort_Rate_ClassesJob>()
        .AddScoped<IServiceJobs, Resort_Room_ClassesJob>()
        .AddScoped<IServiceJobs, Computed_CommissionsJob>()
        .AddScoped<IServiceJobs, Markets_GroupsJob>()
        .AddScoped<IServiceJobs, Applications_User>()
        .AddScoped<IServiceJobs, Articles_Codes>()
        .AddScoped<IServiceJobs, Department>()
        .AddScoped<IServiceJobs, Entity_Detail>()
        .AddScoped<IServiceJobs, Market_Codes_Template>()
        .AddScoped<IServiceJobs, Priorities>()
        .AddScoped<IServiceJobs, Rate_Classes_Template>()
        .AddScoped<IServiceJobs, Rate_Header>()
        .AddScoped<IServiceJobs, Reservation_Daily_Element_Name>()
        .AddScoped<IServiceJobs, Reservation_Daily_Elements>()
        .AddScoped<IServiceJobs, Reservation_Promotions>()
        .AddScoped<IServiceJobs, Resort>()
        .AddScoped<IServiceJobs, Room>()
        .AddScoped<IServiceJobs, Work_Orders>()
        .AddScoped<IServiceJobs, Membership_Types>()
        .AddScoped<IServiceJobs, Memberships>()
        .AddScoped<IServiceJobs, Namesref>()
        .AddScoped<IServiceJobs, Preferences>()
        .AddScoped<IServiceJobs, Reservation_Special_Requests>()
        .AddScoped<IServiceJobs, Reservation_Comment>()
        .AddScoped<IServiceJobs, Resort_Origins_Of_Booking>()
        .AddScoped<IServiceJobs, RoomsCombo>()
        .AddScoped<IServiceJobs, Room_Classes_Template>()

        //#### TRIGGERS #####
        .AddSingleton<ITriggers, IntervaloTempo>()
        .AddSingleton<ITriggers, ExecucaoUnica>()
        .AddSingleton<ITriggers, UnicoDiadoMes>()
        .AddSingleton<ITriggers, DiasdaSemana>()

        //#### DB CONTEXT #####
        .AddSingleton<DapperContext>()
        .AddTransient<IMain, Main>();

    })
    .Build(); 

host.Run();

//sc create servicowindows binPath=C:\temp\servicowindows.exe