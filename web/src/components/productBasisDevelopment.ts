import type ProductParameterSetup from './ProductParameterSetup.vue'
import type ProductBusinessRuleSetup from './ProductBusinessRuleSetup.vue'
import type ProductRunTimeSetup from './ProductRunTimeSetup.vue'
import type ProductQCItemSetup from './ProductQCItemSetup.vue'
import type ProductPackingSet from './ProductPackingSet.vue'
import type ProductEquipmentCapItems from './ProductEquipmentCapItems.vue'
import type ProductEsopSetup from './ProductEsopSetup.vue'
import type ProductEquipmentSpec from './ProductEquipmentSpec.vue'
import type ProductApsIntegration from './ProductApsIntegration.vue'
import type ProductBinSetup from './ProductBinSetup.vue'
import type ProductDataImport from './ProductDataImport.vue'

export type ProductRow = Record<string, any>
export type ProductParameterData = NonNullable<InstanceType<typeof ProductParameterSetup>['$props']['parameters']>
export type ProductBusinessRuleData = NonNullable<InstanceType<typeof ProductBusinessRuleSetup>['$props']['businessRules']>
export type ProductRunTimeProps = InstanceType<typeof ProductRunTimeSetup>['$props']
export type ProductRunTimeData = NonNullable<ProductRunTimeProps['setup']>
export type ProductRunTimeRow = ProductRunTimeProps['defaultRows'][number]
export type ProductQCAssignment = Parameters<InstanceType<typeof ProductQCItemSetup>['$props']['saveAssignments']>[1][number]
export type ProductPackingProps = InstanceType<typeof ProductPackingSet>['$props']
export type EquipmentCapRows = InstanceType<typeof ProductEquipmentCapItems>['$props']['rows']
export type ProductEsopRows = InstanceType<typeof ProductEsopSetup>['$props']['rows']
export type ProductEquipmentSpecProps = InstanceType<typeof ProductEquipmentSpec>['$props']
export type ProductApsProps = InstanceType<typeof ProductApsIntegration>['$props']
export type ProductBinProps = InstanceType<typeof ProductBinSetup>['$props']
export type ImportProducts = InstanceType<typeof ProductDataImport>['$props']['importProducts']
export type ProductRelation = ProductPackingProps['relations'][string] & {
  processes: ProductRow[]
  bom: ProductRow[]
  constraints: ProductRow[]
  properties: ProductRow[]
  parameters?: ProductParameterData
  businessRules?: ProductBusinessRuleData
  opDescriptions?: ProductRow[]
  recipeGroups?: ProductRow[]
  accessories?: ProductRow[]
  opAreas?: ProductRow[]
  subOps?: ProductRow[]
  productPrograms?: string[]
  runTime?: ProductRunTimeData
  qcItems?: ProductRow[]
  equipmentCapItems?: EquipmentCapRows
  esopRows?: ProductEsopRows
  equipmentSpecs?: ProductEquipmentSpecProps['savedSpecs']
  apsSetups?: ProductRow[]
  apsSetupDetails?: ProductRow[]
  bins?: ProductBinProps['savedBins']
  materialGroups?: ProductRow[]
}

export const rowsKey = 'ymes.dev.prd-product-basis.v1'
export const relationKey = 'ymes.dev.prd-product-basis-relations.v1'

export const seedRows: ProductRow[] = [
  {
    productNo: 'DEV-PRD-1001', productVersion: '01', productName: 'DEV 控制器組件', productType: 'DEV-FINISHED',
    issueState: 0, curVersion: 0, creator: 'DEV001', createDate: '2026-11-01T09:00:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'DEV-SPEC-01', productCode: 'DEV1001', serialTypeNoLot: 'DEV-LOT-A',
    serialTypeNoComp: 'DEV-COMP-A', pictureName: '', lotStdQty: 0, erpNo: 'DEV-ERP-1001',
    description: '開發用假資料。未核准產品，含流程、BOM、屬性及設備限制示例。',
    qccategory: 'DEV-QC', customerNo: 'DEV-CUSTOMER', deviceNo: 'DEV-DEVICE', ecnControl: 'N',
    sapKtext: 'DEV-PRD-1001-01', sapPlnal: '11', dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-01T09:00:00',
  },
  {
    productNo: 'DEV-PRD-1002', productVersion: '02', productName: 'DEV 核准產品', productType: 'DEV-SEMIFINISHED',
    issueState: 2, curVersion: 1, creator: 'DEV002', createDate: '2026-10-21T10:10:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'N/A', productCode: 'DEV1002', lotStdQty: 120,
    description: '開發用假資料。已核准且為目前版本。',
    dataStamp: 3, reviser: 'DEV002', reviseDate: '2026-11-03T13:20:00', sapPlnal: '21',
  },
  {
    productNo: 'DEV-PRD-1003', productVersion: '01', productName: 'DEV 簽核中產品', productType: 'DEV-FINISHED',
    issueState: 1, curVersion: 0, creator: 'DEV003', createDate: '2026-11-05T11:00:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'N/A', productCode: 'DEV1003', lotStdQty: 0,
    description: '開發用假資料。簽核中不可修改或刪除。',
    dataStamp: 2, reviser: 'DEV003', reviseDate: '2026-11-05T11:00:00', sapPlnal: '51',
  },
  {
    productNo: 'DEV-PRD-1004', productVersion: '01', productName: 'DEV 停用產品', productType: 'DEV-FINISHED',
    issueState: -1, curVersion: 0, creator: 'DEV001', createDate: '2026-08-12T14:00:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'N/A', productCode: 'DEV1004', lotStdQty: 0,
    description: '開發用假資料。不使用狀態可實體刪除。',
    dataStamp: 5, reviser: 'DEV002', reviseDate: '2026-10-02T08:20:00', sapPlnal: '31',
  },
]
export const seedRelations: Record<string, ProductRelation> = {
  'DEV-PRD-1001|01': {
    processes: [{ moTypeNo: 1, psNo: 'DEV-PS-10', psOrder: 10, processNo: 'DEV-PROC-01', processVersion: '01', defaultProcess: 1, haveComponent: 1, haveLevel: 0, equipmentGroup: 'DEV-EQP-GRP' }],
    opDescriptions: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', opName: '點膠作業', description: 'DEV 產品版本作業說明範例。', dataStamp: 2, creator: 'DEV001', createDate: '2026-11-01T09:10:00', reviser: 'DEV001', reviseDate: '2026-11-04T10:30:00' },
    ],
    recipeGroups: [{ productNo: 'DEV-PRD-1001', productVersion: '01', recipeGroup: 'DEV-RG-01' }],
    accessories: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', opName: '點膠作業', accessoryType: 'DEV-ACC-FIXTURE', accessoryNo: 'DEV-JIG-001' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', opName: '點膠作業', accessoryType: 'DEV-ACC-FIXTURE', accessoryNo: 'DEV-JIG-003' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-20', opName: '組裝作業', accessoryType: 'DEV-ACC-CONSUMABLE', accessoryNo: '*' },
    ],
    opAreas: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', areaNo: 'DEV-AREA-01', defaultArea: 1, areaName: '點膠區', factoryNo: 'DEV-FAB-01' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', areaNo: 'DEV-AREA-04', defaultArea: 0, areaName: '待驗區', factoryNo: 'DEV-FAB-02' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-20', areaNo: 'DEV-AREA-02', defaultArea: 1, areaName: '組裝區', factoryNo: 'DEV-FAB-01' },
    ],
    subOps: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', subOpSequence: 1, subOpNo: 'DEV-SUB-10-01', subOpName: '點膠前清潔', creator: 'DEV001', createDate: '2026-11-01T09:12:00', dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-04T10:30:00' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', subOpSequence: 2, subOpNo: 'DEV-SUB-10-02', subOpName: '塗佈膠材', creator: 'DEV001', createDate: '2026-11-01T09:15:00', dataStamp: 1, reviser: '', reviseDate: '' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-20', subOpSequence: 1, subOpNo: 'DEV-SUB-20-01', subOpName: '外觀組裝確認', creator: 'DEV001', createDate: '2026-11-01T09:20:00', dataStamp: 0, reviser: '', reviseDate: '' },
    ],
    productPrograms: ['DEV-PROG-01', 'DEV-PROG-03'],
    equipmentCapItems: [
      { opNo: 'DEV-OP-10', capItem: 'DEV-CAP-DISPENSER', capValue: 'HIGHPRECISION', description: 'DEV 點膠設備能力範例',
        creator: 'DEV001', createDate: '2026-11-01T09:10:00', reviser: '', reviseDate: '', dataStamp: 0 },
    ],
    qcItems: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', qcItemNo: 'DEV-QC-VISUAL', qcItemType: 0, executeOrder: 1, inLine: 1, inspTypeFAI: 1, inspTypeIPQC: 1, inspTypePQC: 1, reviser: 'DEV001', reviseDate: '2026-11-04T10:30:00' },
    ],
    apsSetups: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', processNo: 'DEV-PROC-01', processVersion: '01', moTypeNo: 1, opNo: 'DEV-OP-10', nodeId: 'DEV-NODE-10', equipmentNo: 'N/A', equipmentGroup: 'DEV-EQP-GRP', description: 'DEV APS 預設', subcontractorNo: 'N/A', schedule: 0, apsFixEqpTime: 0, apsVarEqpTime: 0, stdWorkTimeQty: 10, transferQty: 0, lotRearTime: 0, batchOp: 0, batchMaxQty: 999999 },
    ],
    apsSetupDetails: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', processNo: 'DEV-PROC-01', processVersion: '01', moTypeNo: 1, opNo: 'DEV-OP-10', nodeId: 'DEV-NODE-10', equipmentNo: 'DEV-EQP-01', equipmentGroup: 'DEV-EQP-GRP', description: 'DEV APS 設備明細', schedule: 0, apsFixEqpTime: 0, apsVarEqpTime: 0, stdWorkTimeQty: 10, transferQty: 0, lotRearTime: 0, batchOp: 0, batchMaxQty: 999999 },
    ],
    bom: [
      { nodeNo: 'DEV-MTL-001', nodeType: 0, nodeVersion: '001', opNo: 'DEV-OP-10', stdQty: 2, unitNo: 'EA', decreaseRate: 0, specified: 1, description: 'DEV 材料明細' },
      { nodeNo: 'DEV-PRD-SUB', nodeType: 1, nodeVersion: '01', opNo: 'DEV-OP-20', stdQty: 1, unitNo: 'EA', decreaseRate: 0.02, specified: 0, description: 'DEV 子產品節點' },
    ],
    constraints: [{ opNo: 'DEV-OP-10', equipmentNo: 'DEV-EQP-01', description: '限定設備示例', creator: 'DEV001', createDate: '2026-11-01T09:00:00' }],
    properties: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', propertyNo: 'DEV-MODEL', controlled: 1, defaultValue: 'CTRL-A', propertySequence: 1, description: 'DEV 型號屬性', issueState: 0, dataStamp: 1, creator: 'DEV001', createDate: '2026-10-01T08:30:00', reviser: '', reviseDate: '' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', propertyNo: 'DEV-COLOR', controlled: 2, defaultValue: '', propertySequence: 2, description: 'DEV 顏色提示屬性', issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-10-01T08:31:00', reviser: '', reviseDate: '' },
    ],
    parameters: {
      ops: [
        { serialNo: 'DEV-SER-OP-10', psNo: 'DEV-PS-10', opNo: 'DEV-OP-10', opName: '點膠作業', opOrder: 10, haveComponent: 1, nodeId: '', opScript: '', dataStamp: 1 },
        { serialNo: 'DEV-SER-OP-20', psNo: 'DEV-PS-10', opNo: 'DEV-OP-20', opName: '組裝作業', opOrder: 20, haveComponent: 1, nodeId: '', opScript: '', dataStamp: 1 },
      ],
      attributes: [
        { serialNo: 'DEV-SER-OP-10', attribType: 0, attribNo: 'DEV-LOT-QTY', attribValue: '24', attribSequence: 1, attribSource: 3, attribPhase: 1, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' },
        { serialNo: 'DEV-SER-OP-10', attribType: 0, attribNo: 'DEV-LOT-TEMP', attribValue: '', attribSequence: 2, attribSource: 0, attribPhase: 2, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' },
        { serialNo: 'DEV-SER-OP-10', attribType: 1, attribNo: 'DEV-COMP-LOT', attribValue: '', attribSequence: 1, attribSource: 0, attribPhase: 1, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' },
      ],
      rules: [{ serialNo: 'DEV-SER-OP-10', ruleNo: 'DEV-RULE-LOT-001', ruleSequence: 1, rulePhase: 2, ruleScript: 'return lot.QTY > 0;', ruleType: 0, ruleClass: 0 }],
      equipments: [{ serialNo: 'DEV-SER-OP-10', psNo: 'DEV-PS-10', opNo: 'DEV-OP-10', equipmentNo: 'DEV-EQP-01', recipeGroup: 'DEV-RECIPE-GROUP-A', recipes: [{ recipeNo: 'DEV-RECIPE-001', recipeValue: 'DEV-SET-01' }] }],
    },
    businessRules: { operations: [{ opNo: 'DEV-OP-10', opName: '點膠作業', processNo: 'DEV-PROC-01', processVersion: '01', nodes: [
      { id: 'DEV-BR-START', nodeNo: 'START', kind: 'start', x: 70, y: 155 },
      { id: 'DEV-BR-001', nodeNo: 'DEV-BR-001', kind: 'rule', x: 270, y: 155, phaseNo: 1, phaseName: 'Before OP' },
      { id: 'DEV-BR-002', nodeNo: 'DEV-BR-002', kind: 'rule', x: 500, y: 155, phaseNo: 2, phaseName: 'During OP' },
      { id: 'DEV-BR-END', nodeNo: 'END', kind: 'end', x: 720, y: 155 },
    ], links: [
      { id: 'DEV-BRL-1', fromNode: 'DEV-BR-START', toNode: 'DEV-BR-001', linkName: 'pass', lineStyle: 'VH', phaseNo: 99 },
      { id: 'DEV-BRL-2', fromNode: 'DEV-BR-001', toNode: 'DEV-BR-002', linkName: 'pass', lineStyle: 'HV', phaseNo: 1 },
      { id: 'DEV-BRL-3', fromNode: 'DEV-BR-002', toNode: 'DEV-BR-END', linkName: 'pass', lineStyle: 'polyline', phaseNo: 2 },
    ] }] },
    materialGroups: [{ nodeNo: 'DEV-MTL-001', opNo: 'DEV-OP-10', materialNo: 'DEV-MTL-GROUP-A' }, { nodeNo: 'DEV-MTL-001', opNo: 'DEV-OP-10', materialNo: 'DEV-MTL-GROUP-B' }],
  },
  'DEV-PRD-SUB|01': {
    processes: [],
    bom: [
      { nodeNo: 'DEV-MTL-SUB-01', nodeType: 0, nodeVersion: '001', opNo: 'DEV-OP-30', stdQty: 4, unitNo: 'EA', decreaseRate: 0, specified: 1, description: 'DEV 子產品材料' },
      { nodeNo: 'DEV-PRD-COMP', nodeType: 1, nodeVersion: '02', opNo: 'DEV-OP-40', stdQty: 1, unitNo: 'EA', decreaseRate: 0.01, specified: 0, description: 'DEV 第二層子產品' },
    ],
    constraints: [], properties: [],
  },
  'DEV-PRD-COMP|02': {
    processes: [],
    bom: [{ nodeNo: 'DEV-MTL-COMP-01', nodeType: 0, nodeVersion: '001', opNo: 'DEV-OP-50', stdQty: 2, unitNo: 'EA', decreaseRate: 0.005, specified: 1, description: 'DEV 第三層材料' }],
    constraints: [], properties: [],
  },
  'DEV-PRD-1002|02': { processes: [], bom: [], constraints: [], properties: [] },
  'DEV-PRD-1003|01': { processes: [], bom: [], constraints: [], properties: [] },
  'DEV-PRD-1004|01': { processes: [], bom: [], constraints: [], properties: [] },
}
