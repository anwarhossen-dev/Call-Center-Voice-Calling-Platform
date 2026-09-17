import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';

export interface TableSummary {
  id: string;
  name: string;
  displayName: string;
  category: string;
  icon: string;
  description: string;
  rowCount: number;
  columns: string[];
}

@Component({
  selector: 'app-tables-portal',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './tables-portal.component.html',
  styleUrl: './tables-portal.component.css'
})
export class TablesPortalComponent implements OnInit {
  tablesList = signal<TableSummary[]>([]);
  activeCategory = signal<string>('All');
  selectedTableId = signal<string>('users');
  selectedTable = signal<TableSummary | null>(null);

  // Table Data & State
  rows = signal<any[]>([]);
  totalRows = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = signal<number>(15);
  searchQuery = signal<string>('');
  isLoading = signal<boolean>(false);
  errorMessage = signal<string>('');

  // Row Inspector Modal
  inspectModalOpen = signal<boolean>(false);
  inspectedRow = signal<any>(null);

  // Search input binding
  searchInput: string = '';

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['table']) {
        this.selectedTableId.set(params['table']);
      }
      this.loadTablesSummary();
    });
  }

  // Categories computed list
  categories = computed(() => {
    const list = this.tablesList();
    const set = new Set<string>();
    list.forEach(t => set.add(t.category));
    return ['All', ...Array.from(set)];
  });

  // Filtered tables list computed
  filteredTables = computed(() => {
    const list = this.tablesList();
    const cat = this.activeCategory();
    if (cat === 'All') return list;
    return list.filter(t => t.category === cat);
  });

  // Total system records counter
  totalSystemRecords = computed(() => {
    return this.tablesList().reduce((acc, curr) => acc + curr.rowCount, 0);
  });

  loadTablesSummary() {
    this.isLoading.set(true);
    fetch('http://localhost:5000/api/v1/tables')
      .then(res => {
        if (!res.ok) throw new Error('Failed to fetch tables list');
        return res.json();
      })
      .then(data => {
        if (data && data.tables) {
          this.tablesList.set(data.tables);
          // Set initial active table
          const targetId = this.selectedTableId();
          const found = data.tables.find((t: TableSummary) => t.id === targetId) || data.tables[0];
          if (found) {
            this.selectTable(found);
          }
        }
      })
      .catch(err => {
        this.errorMessage.set('Backend connection error: ' + err.message);
      })
      .finally(() => {
        this.isLoading.set(false);
      });
  }

  selectTable(table: TableSummary) {
    this.selectedTableId.set(table.id);
    this.selectedTable.set(table);
    this.currentPage.set(1);
    this.searchInput = '';
    this.searchQuery.set('');
    this.loadTableData();
  }

  loadTableData() {
    const tableId = this.selectedTableId();
    if (!tableId) return;

    this.isLoading.set(true);
    this.errorMessage.set('');

    const page = this.currentPage();
    const size = this.pageSize();
    const search = encodeURIComponent(this.searchQuery());

    fetch(`http://localhost:5000/api/v1/tables/${tableId}?page=${page}&pageSize=${size}&search=${search}`)
      .then(res => {
        if (!res.ok) throw new Error(`Failed to load data for table '${tableId}'`);
        return res.json();
      })
      .then(data => {
        if (data) {
          this.rows.set(data.rows || []);
          this.totalRows.set(data.total || 0);

          // Update rowCount on the table item in tablesList
          const updatedList = this.tablesList().map(t => {
            if (t.id === tableId && !search) {
              return { ...t, rowCount: data.total };
            }
            return t;
          });
          this.tablesList.set(updatedList);
        }
      })
      .catch(err => {
        this.errorMessage.set(err.message);
        this.rows.set([]);
        this.totalRows.set(0);
      })
      .finally(() => {
        this.isLoading.set(false);
      });
  }

  onSearchSubmit() {
    this.searchQuery.set(this.searchInput.trim());
    this.currentPage.set(1);
    this.loadTableData();
  }

  clearSearch() {
    this.searchInput = '';
    this.searchQuery.set('');
    this.currentPage.set(1);
    this.loadTableData();
  }

  setCategory(cat: string) {
    this.activeCategory.set(cat);
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadTableData();
  }

  totalPages = computed(() => {
    const total = this.totalRows();
    const size = this.pageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  inspectRow(row: any) {
    this.inspectedRow.set(row);
    this.inspectModalOpen.set(true);
  }

  closeModal() {
    this.inspectModalOpen.set(false);
    this.inspectedRow.set(null);
  }

  formatValue(val: any): string {
    if (val === null || val === undefined) return '—';
    if (typeof val === 'boolean') return val ? 'True' : 'False';
    if (typeof val === 'object') return JSON.stringify(val);
    return String(val);
  }

  getCellValue(row: any, col: string): any {
    if (!row) return '—';
    if (row[col] !== undefined && row[col] !== null) return row[col];
    const camel = col.charAt(0).toLowerCase() + col.slice(1);
    if (row[camel] !== undefined && row[camel] !== null) return row[camel];
    const lower = col.toLowerCase();
    for (const key of Object.keys(row)) {
      if (key.toLowerCase() === lower && row[key] !== undefined && row[key] !== null) {
        return row[key];
      }
    }
    return '—';
  }

  isDateColumn(col: string): boolean {
    const c = col.toLowerCase();
    return c === 'createdat' || c === 'initiatedat' || c === 'answeredat' || c === 'endedat' || 
           c === 'statechangedat' || c === 'offloadedat' || c === 'hiredate' || c === 'startdate' || 
           c === 'enddate' || c === 'eventtime' || c.endsWith('date');
  }

  isDateValue(val: any, col: string): boolean {
    if (!this.isDateColumn(col)) return false;
    if (typeof val !== 'string') return false;
    return /^\d{4}-\d{2}-\d{2}/.test(val);
  }

  isStatusColumn(col: string): boolean {
    const c = col.toLowerCase();
    return c === 'status' || c === 'currentstate' || c === 'isactive' || c === 'direction';
  }

  exportCsv() {
    const currentTable = this.selectedTable();
    const dataRows = this.rows();
    if (!currentTable || dataRows.length === 0) return;

    const cols = currentTable.columns;
    const headerRow = cols.join(',');
    const bodyRows = dataRows.map(row => {
      return cols.map(col => {
        const val = row[col] ?? row[col.charAt(0).toLowerCase() + col.slice(1)] ?? '';
        const escaped = String(val).replace(/"/g, '""');
        return `"${escaped}"`;
      }).join(',');
    });

    const csvContent = 'data:text/csv;charset=utf-8,' + [headerRow, ...bodyRows].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `${currentTable.name}_Export_${new Date().toISOString().slice(0,10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
