INSERT INTO users (
    aggregateid, createdtime, updatedtime, createdby, updatedby,
    lastaggregatesequencenumber, username, name, email, phone,
    lastpasswordreset, twofactorenabled, status
) VALUES (
    'staff-default-001', NOW(), NOW(), 'system', 'system',
    1, 'test@rest.com', 'System Admin', 'test@rest.com', '+1 (555) 000-0000',
    'Jun 15, 2026', false, 'Active'
) ON CONFLICT (aggregateid) DO NOTHING;
