IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('procySetDeliveryFTEForCM'))
	DROP PROCEDURE procySetDeliveryFTEForCM
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('procSetDeliveryFTEForCM'))
	DROP PROCEDURE procSetDeliveryFTEForCM

	/* DECLARE @tblTempCostCenter dbo.CCTableDataType 
	 INSERT INTO @tblTempCostCenter
	 SELECT '92510790'
	 EXEC procSetDeliveryFTEForCM  5, @tblTempCostCenter */
GO

CREATE PROCEDURE  [dbo].procSetDeliveryFTEForCM
	 @pScenarioId INT  
	,@pCostCentreDataTable dbo.CCTableDataType READONLY
AS
BEGIN
	
	SET NOCOUNT ON
	
	CREATE TABLE #tblCostCentre(
		cost_centre VARCHAR(8) COLLATE DATABASE_DEFAULT
	)

	INSERT INTO #tblCostCentre(cost_centre)
	SELECT cost_centre FROM @pCostCentreDataTable

	CREATE TABLE #TempDummyResource(
		cm_resource_id INT,
		unique_id VARCHAR(64)  COLLATE DATABASE_DEFAULT,
		category VARCHAR(32)  COLLATE DATABASE_DEFAULT,
		sub_group_code VARCHAR(32)  COLLATE DATABASE_DEFAULT
	)

	INSERT INTO #TempDummyResource(cm_resource_id,unique_id,category,sub_group_code)  
	SELECT CMR.cm_resource_id,CMR.employee_category_unique_id,category,position_employee_subgroup_code
	FROM CapacityManagementResources CMR 
	INNER JOIN EmployeeCategoryMapping ECM ON CMR.employee_category_unique_id = ECM.unique_id
	WHERE ISNULL(ECM.position_employee_subgroup_code,'') = ''


	CREATE TABLE #tblDeliveryReportSum(
		cost_centre VARCHAR(8) COLLATE DATABASE_DEFAULT NULL,
		capacity_management_id INT,
		gl_code_id INT,
		amount FLOAT,
		maximum_capacity_hrs FLOAT
	)

	INSERT INTO #tblDeliveryReportSum(cost_centre,capacity_management_id,gl_code_id,amount,maximum_capacity_hrs)
	SELECT DR.cost_centre,
		CM.capacity_management_id,
		DR.gl_code_id,
		SUM(DR.amount) AS amount,
		SUM(DR.number_of_unit * ISNULL(CAST(GL.additional_field_01 AS FLOAT),1)) AS maximum_capacity_hrs
	FROM DeliveryReport DR 
	INNER JOIN #tblCostCentre CC ON DR.cost_centre = CC.cost_centre
		AND DR.delivery_costing_detail_id IS NOT NULL 
		AND DR.scenario_id = @pScenarioId
	INNER JOIN GlCodesUI GL ON DR.gl_code_id = GL.gl_code_id 
	INNER JOIN CapacityManagements CM ON CC.cost_centre = CM.cost_center AND CM.scenario_id = @pScenarioId
	WHERE DR.scenario_id = @pScenarioId 
		AND cost_type = 'Expenses' 
		AND gl_level_1 = 'Employee Related Cost' 
		AND gl_level_2 IN ('Part Time Teaching','Non Teaching')
		AND gl_level_3 NOT IN ('Part Time Non Teaching','Full Time Non Teaching')
	GROUP BY DR.cost_centre,CM.capacity_management_id,DR.gl_code_id

	DELETE CR FROM [CapacityReportPartTimeCasualFTE] CR
	INNER JOIN #tblCostCentre DR ON CR.cost_centre = DR.cost_centre 
		AND CR.scenario_id = @pScenarioId AND CR.gl_code_id IS NOT NULL

	INSERT INTO [dbo].[CapacityReportPartTimeCasualFTE]		  
           ([staff_id],capacity_management_id,[scenario_id],[employee_category_unique_id],employee_id
           ,[sbi_category_1],[sbi_category_2],[category],[gl_code],[cm_resource_id],[cost_centre],[comments]
           ,[full_year_salary],[actual_hour],[hourly_rate],[staff_fte],[yearly_fte],[maximum_capacity_hrs],[adjusted_capacity_hrs]          
		   ,[july_fte],[august_fte],[september_fte],[october_fte],[november_fte],[december_fte]
           ,[january_fte],[february_fte],[march_fte],[april_fte],[may_fte],[june_fte]           
		   ,[july_salary],[august_salary],[september_salary],[october_salary],[november_salary],[december_salary]
           ,[january_salary],[february_salary],[march_salary],[april_salary],[may_salary],[june_salary]           
		   ,[adjusted_full_year_salary],[update_on],[login_employee_id],[gl_code_id])
	SELECT 0 AS [staff_id],DR.capacity_management_id,@pScenarioId,CMR.unique_id,GL.gl_level_3  AS employee_id,
		'Direct' AS [sbi_category_1],'Part Time Teaching' AS [sbi_category_2],
		(CASE WHEN GL.gl_level_2 ='Non Teaching' THEN 'Non Teacher'  ELSE 'Teacher' END) AS [category],
		GL.gl_code,CMR.cm_resource_id,DR.cost_centre,'Dummay Staffs from Delivery' AS [comments],
		DR.amount AS [full_year_salary],DR.maximum_capacity_hrs AS [actual_hour],
		(CASE WHEN DR.maximum_capacity_hrs = 0 THEN 0 ELSE DR.amount / DR.maximum_capacity_hrs END) AS [hourly_rate],
		(
			( DR.maximum_capacity_hrs * 0.01 * SAPGL.january / ( 70 * Payruns.Jan)  )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.february / ( 70 * Payruns.Feb) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.march / ( 70 * Payruns.Mar) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.april / ( 70 * Payruns.Apr) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.may / ( 70 * Payruns.May) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.june / ( 70 * Payruns.Jun) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.july / ( 70 * Payruns.Jul) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.august / ( 70 * Payruns.Aug) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.september / ( 70 * Payruns.Sep) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.october / ( 70 * Payruns.Oct) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.november / ( 70 * Payruns.Nov) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.december / ( 70 * Payruns.[Dec]) )
		) / 12 AS [staff_fte],
		(
			( DR.maximum_capacity_hrs * 0.01 * SAPGL.january / ( 70 * Payruns.Jan)  )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.february / ( 70 * Payruns.Feb) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.march / ( 70 * Payruns.Mar) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.april / ( 70 * Payruns.Apr) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.may / ( 70 * Payruns.May) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.june / ( 70 * Payruns.Jun) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.july / ( 70 * Payruns.Jul) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.august / ( 70 * Payruns.Aug) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.september / ( 70 * Payruns.Sep) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.october / ( 70 * Payruns.Oct) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.november / ( 70 * Payruns.Nov) )
			+ ( DR.maximum_capacity_hrs * 0.01 * SAPGL.december / ( 70 * Payruns.[Dec]) )
		) / 12 AS [yearly_fte],

		DR.maximum_capacity_hrs AS [maximum_capacity_hrs],
		DR.maximum_capacity_hrs AS [adjusted_capacity_hrs],
		   
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.july / ( 70 * Payruns.Jul)) AS [july_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.august / ( 70 * Payruns.Aug)) AS [august_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.september / ( 70 * Payruns.Sep)) AS [september_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.october / ( 70 * Payruns.Oct)) AS [october_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.november / ( 70 * Payruns.Nov)) AS [november_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.december / ( 70 * Payruns.[Dec])) AS [december_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.january / ( 70 * Payruns.Jan)) AS [january_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.february / ( 70 * Payruns.Feb)) AS [february_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.march	 / ( 70 * Payruns.Mar)) AS [march_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.april / ( 70 * Payruns.Apr)) AS [april_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.may / ( 70 * Payruns.May)) AS [may_fte],
		( DR.maximum_capacity_hrs  * 0.01 * SAPGL.june / ( 70 * Payruns.Jun)) AS [june_fte],

		DR.amount * 0.01 * SAPGL.july AS [july_salary],
		DR.amount * 0.01 * SAPGL.august AS [august_salary],
		DR.amount * 0.01 * SAPGL.september AS [september_salary],
		DR.amount * 0.01 * SAPGL.october AS [october_salary],
		DR.amount * 0.01 * SAPGL.november AS [november_salary],
		DR.amount * 0.01 * SAPGL.december AS [december_salary],
		DR.amount * 0.01 * SAPGL.january AS [january_salary],
		DR.amount * 0.01 * SAPGL.february AS [february_salary],
		DR.amount * 0.01 * SAPGL.march AS [march_salary],
		DR.amount * 0.01 * SAPGL.april AS [april_salary],
		DR.amount * 0.01 * SAPGL.may AS [may_salary],
		DR.amount * 0.01 * SAPGL.june AS [june_salary],
		DR.amount AS [adjusted_full_year_salary],
		GETDATE() AS [update_on],
		'System' AS [login_employee_id],
		DR.gl_code_id AS [gl_code_id]

	FROM #tblDeliveryReportSum DR 
	INNER JOIN GlCodesUI GL ON DR.gl_code_id = GL.gl_code_id
	INNER JOIN SAPGLApplicableSpread SAPAGL ON GL.gl_code = SAPAGL.gl_code 
		AND SAPAGL.scenario_id = @pScenarioId
	INNER JOIN SAPGLSpread SAPGL ON SAPAGL.applicable_spread_code = SAPGL.spread_code
	INNER JOIN #TempDummyResource CMR ON CMR.unique_id = GL.gl_level_3
		AND CMR.category = CASE WHEN GL.gl_level_2 LIKE '%Non%' THEN 'Non Teacher' ELSE 'Teacher' END
	INNER JOIN (
				SELECT [Jul],[Aug],[Sep],[Oct],[Nov],[Dec],[Jan],[Feb],[Mar],[Apr],[May],[Jun],@pScenarioId AS Scenario
				FROM (
						SELECT month_name,count_period 
						FROM MonthlyCalendarView MV
						INNER JOIN Years Y on MV.financial_year = Y.year_name
						INNER JOIN Scenarios S on Y.year_id = S.year_id
						WHERE S.scenario_id = @pScenarioId 
					) AS D
				PIVOT (
					MAX(count_period)
					FOR d.month_name 
					IN ( [Jul],[Aug],[Sep],[Oct],[Nov],[Dec],[Jan],[Feb],[Mar],[Apr],[May],[Jun] )
				) AS PVT
	) AS Payruns ON Payruns.Scenario = SAPAGL.scenario_id
	WHERE ISNULL(sub_group_code,'') = ''
		AND cost_type = 'Expenses'
		AND gl_level_1 = 'Employee Related Cost'
		AND gl_level_2 IN ('Part Time Teaching','Non Teaching')
		AND gl_level_3 NOT IN ('Part Time Teaching (CP)','Part Time Non Teaching','Full Time Non Teaching')

	UPDATE CM SET CM.delivery_non_teaching_fte = DR.delivery_non_teaching_fte,
		CM.delivery_teaching_fte = DR.delivery_teaching_fte,
		CM.delivery_non_teaching_salary = DR.delivery_non_teaching_salary,
		CM.delivery_teaching_salary = DR.delivery_teaching_salary,
		
		total_fte = teaching_fte + non_teaching_fte + indirect_fte 
					+ ISNULL( DR.delivery_non_teaching_fte, 0) + ISNULL( DR.delivery_teaching_fte, 0),
		total_adjusted_salary = teaching_salary + non_teaching_salary + indirect_salary
					+ ISNULL( DR.delivery_non_teaching_salary, 0) + ISNULL( DR.delivery_teaching_salary, 0)

	FROM CapacityManagements CM
	INNER JOIN 
	(
		SELECT CR.cost_centre,
			SUM(CASE WHEN GL.gl_level_2 LIKE '%Non%' 
					THEN CR.staff_fte ELSE 0 
				END) AS delivery_non_teaching_fte,
			SUM(CASE WHEN GL.gl_level_2 NOT LIKE '%Non%' 
					THEN CR.staff_fte ELSE 0 
				END) AS delivery_teaching_fte,
			SUM(CASE WHEN GL.gl_level_2 LIKE '%Non%' 
					THEN CR.adjusted_full_year_salary ELSE 0 
				END) AS delivery_non_teaching_salary,
			SUM(CASE WHEN GL.gl_level_2 NOT LIKE '%Non%'
					THEN CR.adjusted_full_year_salary ELSE 0 
				END) AS delivery_teaching_salary,
			SUM( CR.staff_fte ) AS total_fte,
			SUM(CR.adjusted_full_year_salary) AS total_adjusted_salary
		FROM [CapacityReportPartTimeCasualFTE] CR 
		INNER JOIN #tblCostCentre DR ON CR.cost_centre = DR.cost_centre 
			AND CR.scenario_id = @pScenarioId
		INNER JOIN GlCodesUI GL ON CR.gl_code_id = GL.gl_code_id 
		GROUP BY CR.cost_centre

	) AS DR ON CM.cost_center = DR.cost_centre
	WHERE CM.scenario_id = @pScenarioId 


	DELETE CR
	FROM CapacityReport CR 
	INNER JOIN #tblCostCentre DR ON CR.cost_centre = DR.cost_centre 
		AND CR.scenario_id = @pScenarioId
	WHERE CR.gl_code_id IS NOT NULL
		
	INSERT INTO [dbo].[CapacityReport] ([staff_id],[scenario_id],[employee_category_unique_id],[sbi_category_1],[sbi_category_2]
				,[category],[gl_code],[capacity_management_id],[cm_resource_id],[cost_centre],[last_name],[first_name],[middle_name]
				,[pay_scale],[employee_grouping],[position],[comments],[full_year_salary],[actual_hour],[hourly_rate],[staff_fte]
				,[maximum_capacity_hrs],[unavailable_and_unlisted_hour],[adjusted_capacity_hrs],[cost_centre_percentage],[total_percentage]
				,[position_id],[employee_id],[employee_name],[joining_new_in_this_fy],[month_of_joining],[leaving_permanently_this_fy]
				,[month_of_leaving],[planned_allowance_1],[month_of_disbursement_1],[allowance_ammount_1],[planned_allowance_2]
				,[month_of_disbursement_2],[allowance_ammount_2],[salary_adj_needed],[apply_from],[rate_of_adjustment]
				,[july_fte],[august_fte],[september_fte],[october_fte],[november_fte],[december_fte]
				,[january_fte],[february_fte],[march_fte],[april_fte],[may_fte],[june_fte],[july_salary]
				,[august_salary],[september_salary],[october_salary],[november_salary],[december_salary]
				,[january_salary],[february_salary],[march_salary],[april_salary],[may_salary],[june_salary]
				,[july_allowance],[august_allowance],[september_allowance],[october_allowance],[november_allowance],[december_allowance]
				,[january_allowance],[february_allowance],[march_allowance],[april_allowance],[may_allowance],[june_allowance]
				,[capacity_pct],[adjusted_full_year_salary],[adjusted_full_year_allowance],[update_on],[is_from_staff],[employee_subgrouping]
				,[default_fte],[yearly_fte],[login_employee_id],[actual_capacity_report_id],[gl_code_id])
	SELECT [staff_id],[scenario_id],[employee_category_unique_id],[sbi_category_1],[sbi_category_2]
			,[category],[gl_code],[capacity_management_id],[cm_resource_id],CR.[cost_centre],[last_name],[first_name],[middle_name]
			,[pay_scale],[employee_grouping],[position],[comments],[full_year_salary],[actual_hour],[hourly_rate],[staff_fte]
			,[maximum_capacity_hrs],[unavailable_and_unlisted_hour],[adjusted_capacity_hrs],[cost_centre_percentage],[total_percentage]
			,[position_id],[employee_id],[employee_name],[joining_new_in_this_fy],[month_of_joining],[leaving_permanently_this_fy]
			,[month_of_leaving],[planned_allowance_1],[month_of_disbursement_1],[allowance_ammount_1],[planned_allowance_2]
			,[month_of_disbursement_2],[allowance_ammount_2],[salary_adj_needed],[apply_from],[rate_of_adjustment]
			,[july_fte],[august_fte],[september_fte],[october_fte],[november_fte],[december_fte]
			,[january_fte],[february_fte],[march_fte],[april_fte],[may_fte],[june_fte]
			,[july_salary],[august_salary],[september_salary],[october_salary],[november_salary],[december_salary]
			,[january_salary],[february_salary],[march_salary],[april_salary],[may_salary],[june_salary]
			,[july_allowance],[august_allowance],[september_allowance],[october_allowance],[november_allowance],[december_allowance]
			,[january_allowance],[february_allowance],[march_allowance],[april_allowance],[may_allowance],[june_allowance]
			,[capacity_pct],[adjusted_full_year_salary],[adjusted_full_year_allowance],[update_on],[is_from_staff],[employee_subgrouping]
			,[default_fte],[yearly_fte],[login_employee_id],[actual_capacity_report_id],[gl_code_id]
	FROM [CapacityReportPartTimeCasualFTE] CR 
	INNER JOIN #tblCostCentre DR ON CR.cost_centre = DR.cost_centre 
		AND CR.scenario_id = @pScenarioId

	

	DROP TABLE #TempDummyResource
	DROP TABLE #tblCostCentre
	DROP TABLE #tblDeliveryReportSum
	--DROP TABLE #tblAvailableHrsAmount

END