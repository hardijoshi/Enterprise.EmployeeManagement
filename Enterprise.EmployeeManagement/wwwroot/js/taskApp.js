var app = angular.module('taskApp', []);

app.filter('customDate', ['$filter', function ($filter) {
    return function (input) {
        if (!input) return 'Not set';
        try {
            const date = new Date(input);
            if (isNaN(date.getTime())) return 'Not set';
            return date.toLocaleString();
        } catch (e) {
            return 'Not set';
        }
    };
}]);

app.controller('taskController', function ($scope, $http) {

    $http.get('/api/user/current')  // This endpoint should return the current user's data (role, name, etc.)
        .then(function (response) {
            // Successfully fetched user data, update the scope
            $scope.currentUser = response.data;
            console.log('Current User:', $scope.currentUser);  // For debugging purposes
        })
        .catch(function (error) {
            console.error('Error fetching current user:', error);
        });

    $scope.successMessage = null; // Variable to store success message

    // This function will be used to clear the success message
    $scope.clearSuccessMessage = function () {
        $scope.successMessage = null;
    };

    console.log('Current User:', $scope.currentUser);

    $scope.formTitle = "Add New Task";
    $scope.buttonText = "Save Task";
    $scope.tasks = [];
    $scope.employees = [];
    $scope.assignedEmployees = [];
    $scope.reviewers = [];
    $scope.selectedTask = null;
    $scope.newStatus = null;
    $scope.statusUpdateError = null;

    $scope.cleanupModal = function (modalId) {
        $(modalId).modal('hide');
        $('body').removeClass('modal-open');
        $('.modal-backdrop').remove();
        $('body').css('padding-right', '');
    };

    $scope.prepareStatusUpdate = function (task) {
        $scope.selectedTask = task;
        $scope.newStatus = task.status;
        $scope.statusUpdateError = null;
    };

    $scope.resetForm = function () {
        $scope.task = {
            taskId: 0,
            title: '',
            description: '',
            isCompleted: false,
            assignedEmployeeId: '',
            reviewerId: ''
        };
        $scope.formTitle = "Add New Task";
        $scope.buttonText = "Add Task";
    };

    $scope.loadData = function () {
        $http.get('/api/employees')
            .then(function (response) {
                $scope.employees = response.data;
                $scope.assignedEmployees = $scope.employees.filter(emp => emp.role === 'Employee');
                $scope.reviewers = $scope.employees.filter(emp => emp.role === 'Manager');
            })
            .catch(function (error) {
                console.error('Error loading employees:', error);
            });

        $scope.loadTasks();
    };

    $scope.loadTasks = function () {
        $http.get('/api/Tasks')
            .then(function (response) {
                $scope.tasks = response.data;
            })
            .catch(function (error) {
                console.error('Error loading tasks:', error);
            });
    };

    $scope.validateTask = function () {
        if (!$scope.task.title?.trim()) {
            alert('Title is required');
            return false;
        }
        if (!$scope.task.assignedEmployeeId) {
            alert('Please select an assigned employee');
            return false;
        }
        if (!$scope.task.reviewerId) {
            alert('Please select a reviewer');
            return false;
        }
        return true;
    };

    $scope.saveTask = function () {
        if (!$scope.validateTask()) {
            return;
        }

        var taskData = {
            taskId: $scope.task.taskId,
            title: $scope.task.title.trim(),
            description: $scope.task.description?.trim() || '',
            isCompleted: $scope.task.isCompleted || false,
            assignedEmployeeId: parseInt($scope.task.assignedEmployeeId),
            reviewerId: parseInt($scope.task.reviewerId)
        };

        console.log('Sending task data:', taskData);

        // Choose POST or PUT based on taskId
        const isNewTask = taskData.taskId === 0;
        const method = isNewTask ? 'POST' : 'PUT';
        const url = isNewTask ? '/api/Tasks' : `/api/Tasks/${taskData.taskId}`;

        $http({
            method: method,
            url: url,
            data: taskData
        })
            .then(function (response) {
                console.log('Save response:', response);
                $scope.loadTasks();
                $scope.successMessage = 'Task created successfully!';

                // Close the modal after a short delay
                setTimeout(function () {
                    $scope.cleanupModal('#taskModal');
                }, 1500);

                $scope.resetForm();
                //alert('Task saved successfully!');
            })
            .catch(function (error) {
                console.error('Error saving task:', error);
                let errorMessage = 'Error saving task: ';
                if (error.data && error.data.errors) {
                    errorMessage += Object.values(error.data.errors).join(', ');
                } else if (error.data && error.data.title) {
                    errorMessage += error.data.title;
                } else if (error.data) {
                    errorMessage += error.data;
                } else {
                    errorMessage += 'Please try again.';
                }
                alert(errorMessage);
            });
    };

    $scope.editTask = function (task) {
        console.log('Editing task:', task); // Debug log
        $scope.task = {
            taskId: task.taskId,
            title: task.title,
            description: task.description || '',
            isCompleted: task.isCompleted,
            assignedEmployeeId: task.assignedEmployeeId.toString(), // Changed from task.assignedEmployee.id
            reviewerId: task.reviewerId.toString()  // Changed from task.reviewer.id
        };
        $scope.formTitle = "Edit Task";
        $scope.buttonText = "Update Task";

        // Open modal if you're using Bootstrap modal
        $('#taskModal').modal('show');
    };

    $scope.deleteTask = function (taskId) {
        if (confirm('Are you sure you want to delete this task?')) {
            $http.delete('/api/Tasks/' + taskId)
                .then(function () {
                    $scope.loadTasks();
                    alert('Task deleted successfully!');
                })
                .catch(function (error) {
                    console.error('Error deleting task:', error);
                    alert('Error deleting task. Please try again.');
                });
        }
    };
    //$scope.updateTaskStatus = function (task) {
    //    // Example logic: Toggle the task's completion status
    //    task.isCompleted = !task.isCompleted;

    //    // Simulate a backend call (replace this with actual API call)
    //    console.log(`Task ${task.title} status updated to: ${task.isCompleted ? 'Completed' : 'Pending'}`);
    //};

    $scope.getStatusText = function (status) {
        switch (parseInt(status)) {
            case 0:
                return 'Not Started';
            case 1:
                return 'Working';
            case 2:
                return 'Pending';
            case 3:
                return 'Completed';
            default:
                return 'Unknown';
        }
    };

    $scope.isValidTransition = function (newStatus) {
        if (!$scope.selectedTask) return false;

        const currentStatus = parseInt($scope.selectedTask.status);
        newStatus = parseInt(newStatus);

        switch (currentStatus) {
            case 0: // NotStarted
                return newStatus === 1; // Can only move to Working
            case 1: // Working
                return newStatus === 2 || newStatus === 3; // Can move to Pending or Completed
            case 2: // Pending
                return newStatus === 1 || newStatus === 3; // Can move to Working or Completed
            case 3: // Completed
                return false; // Cannot move from Completed
            default:
                return false;
        }
    };

    $scope.updateTaskStatus = function () {
        if (!$scope.selectedTask || !$scope.newStatus) return;

        console.log('Updating status for task:', $scope.selectedTask);
        console.log('New status:', $scope.newStatus);

        $http.patch(`/api/tasks/${$scope.selectedTask.taskId}/status`, $scope.newStatus)
            .then(function (response) {
                console.log('Status update response:', response);

                // Force status change date update
                $scope.selectedTask.statusChangeDate = new Date().toISOString();
                $scope.selectedTask.status = $scope.newStatus;

                $scope.loadTasks();
                $scope.successMessage = 'Status Updated successfully!';

                setTimeout(function () {
                    $scope.cleanupModal('#statusModal');
                }, 1500);
            })
            .catch(function (error) {
                console.error('Status update error:', error);
                $scope.statusUpdateError = error.data || 'Error updating task status';
            });
    };

    $scope.getStatusText = function (status) {
        switch (parseInt(status)) {
            case 0: return 'Not Started';
            case 1: return 'Working';
            case 2: return 'Pending';
            case 3: return 'Completed';
            default: return 'Unknown';
        }
    };

    $scope.getDaysInStatus = function (task) {
        if (!task?.statusChangeDate) return 0;

        try {
            const changeDate = new Date(task.statusChangeDate);
            if (isNaN(changeDate.getTime())) return 0;

            const now = new Date();
            const diffTime = Math.abs(now - changeDate);
            return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        } catch (e) {
            console.error('Error calculating days in status:', e);
            return 0;
        }
    };


    // Initialize the page
    $scope.resetForm();
    $scope.loadData();
});

