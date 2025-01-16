// Wait for the document to be fully loaded
$(document).ready(function () {
    // Initialize DataTables on the table with ID 'example'
    $('employeeTable').DataTable({

        paging: true,
        searching: true,
        ordering: true,
        info: true,
        scrollCollapse: true,
        scroller: true,
        scrollY: 200,
        responsive: true,
        lengthMenu: [5, 10, 25, 50],
        pageLength: 10,
        columnDefs: [
            {
                targets: [3],
                orderable: false,
            },
        ],
        language: {
            search: "Search",
        },
        rowCallback: function (row, data, index) {
            // Apply background color logic
            if (index % 2 === 0) { // Alternate rows
                $(row).css("background-color", "#74B9FF"); // Light gray for even rows
            } else {
                $(row).css("background-color", "#ffffff"); // White for odd rows
            }
        },
    });
});
