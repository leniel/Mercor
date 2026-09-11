import React from 'react';
import Button from '@mui/material/Button';

const TodoImport = ({ onImport }) =>
{
    const selectFile = event =>
    {
        const file = event.target.files[0];

        if (file)
        {
            onImport(file);
        }

        event.target.value = '';
    };

    return (
        <div style={{ padding: 16 }}>
            <input
                accept=".csv,text/csv"
                aria-label="Import CSV file"
                id="todo-csv-import"
                onChange={selectFile}
                style={{ display: 'none' }}
                type="file"
            />
            <label htmlFor="todo-csv-import">
                <Button component="span" variant="contained">
                    Import CSV
                </Button>
            </label>
        </div>
    );
};

export default TodoImport;
