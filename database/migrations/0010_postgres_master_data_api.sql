BEGIN;

-- The legacy schema has no product-to-process or product BOM relation table, and
-- its process node type encoding is not documented. Keep these UI-owned relations
-- in clearly named YMES tables instead of assigning new meanings to MES columns.
CREATE TABLE IF NOT EXISTS "YMES".ym_prsprocess_node (
    processno varchar(30) NOT NULL,
    processversion varchar(3) NOT NULL,
    nodeid varchar(50) NOT NULL,
    nodeno varchar(50) NOT NULL,
    nodetype varchar(10) NOT NULL,
    sequence integer NOT NULL,
    operationno varchar(20),
    description varchar(255) NOT NULL DEFAULT '',
    CONSTRAINT pk_ym_prsprocess_node PRIMARY KEY (processno, processversion, nodeid),
    CONSTRAINT uq_ym_prsprocess_node_no UNIQUE (processno, processversion, nodeno),
    CONSTRAINT uq_ym_prsprocess_node_sequence UNIQUE (processno, processversion, sequence),
    CONSTRAINT fk_ym_prsprocess_node_process FOREIGN KEY (processno, processversion)
        REFERENCES "YMES".tblprsprocessbasis (processno, processversion) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "YMES".ym_prsprocess_edge (
    processno varchar(30) NOT NULL,
    processversion varchar(3) NOT NULL,
    from_node_id varchar(50) NOT NULL,
    to_node_id varchar(50) NOT NULL,
    action varchar(255) NOT NULL DEFAULT '',
    CONSTRAINT pk_ym_prsprocess_edge PRIMARY KEY (processno, processversion, from_node_id, to_node_id, action),
    CONSTRAINT ck_ym_prsprocess_edge_not_self CHECK (from_node_id <> to_node_id),
    CONSTRAINT fk_ym_prsprocess_edge_from FOREIGN KEY (processno, processversion, from_node_id)
        REFERENCES "YMES".ym_prsprocess_node (processno, processversion, nodeid) ON DELETE CASCADE,
    CONSTRAINT fk_ym_prsprocess_edge_to FOREIGN KEY (processno, processversion, to_node_id)
        REFERENCES "YMES".ym_prsprocess_node (processno, processversion, nodeid) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "YMES".ym_product_process (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    motypeno numeric(2,0) NOT NULL,
    production_segment_no varchar(40) NOT NULL,
    segment_order integer NOT NULL,
    processno varchar(30) NOT NULL,
    process_version_selector varchar(3) NOT NULL,
    default_process boolean NOT NULL DEFAULT false,
    have_component boolean NOT NULL DEFAULT false,
    have_level boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_ym_product_process PRIMARY KEY
        (productno, productversion, motypeno, production_segment_no, processno, process_version_selector),
    CONSTRAINT fk_ym_product_process_product FOREIGN KEY (productno, productversion)
        REFERENCES "YMES".tblprdproductbasis (productno, productversion) ON DELETE CASCADE,
    CONSTRAINT ck_ym_product_process_selector CHECK (process_version_selector = '*' OR process_version_selector <> '')
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_ym_product_process_default
    ON "YMES".ym_product_process (productno, productversion, motypeno, production_segment_no)
    WHERE default_process;

CREATE TABLE IF NOT EXISTS "YMES".ym_product_bom (
    id uuid NOT NULL,
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    component_type varchar(10) NOT NULL,
    component_no varchar(50) NOT NULL,
    input_operation_no varchar(20) NOT NULL,
    standard_qty numeric(12,4) NOT NULL,
    put_in_place_type varchar(40) NOT NULL DEFAULT '',
    specified boolean NOT NULL DEFAULT false,
    decrease_rate numeric(5,2) NOT NULL DEFAULT 0,
    description varchar(1000) NOT NULL DEFAULT '',
    CONSTRAINT pk_ym_product_bom PRIMARY KEY (id),
    CONSTRAINT uq_ym_product_bom_component UNIQUE
        (productno, productversion, component_type, component_no, input_operation_no),
    CONSTRAINT fk_ym_product_bom_product FOREIGN KEY (productno, productversion)
        REFERENCES "YMES".tblprdproductbasis (productno, productversion) ON DELETE CASCADE,
    CONSTRAINT ck_ym_product_bom_component_type CHECK (component_type IN ('MATERIAL', 'PRODUCT')),
    CONSTRAINT ck_ym_product_bom_qty CHECK (standard_qty > 0),
    CONSTRAINT ck_ym_product_bom_decrease_rate CHECK (decrease_rate BETWEEN 0 AND 100)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblmtlmaterialbasis,
    "YMES".tblprsprocessbasis,
    "YMES".tblprdproductbasis,
    "YMES".ym_prsprocess_node,
    "YMES".ym_prsprocess_edge,
    "YMES".ym_product_process,
    "YMES".ym_product_bom
TO ymes_app;

COMMIT;
